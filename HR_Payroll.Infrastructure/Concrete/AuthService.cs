using Dapper;
using HR_Payroll.Core.Entity;
using HR_Payroll.Core.Model.Auth;
using HR_Payroll.Core.Services;
using HR_Payroll.Infrastructure.Data;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace HR_Payroll.Infrastructure.Concrete
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, ILogger<AuthService> logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Result<sp_UserLogin>> UserLoginAsync(LoginModel request)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                // Ensure connection is open
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@LoginIdentifier", request.Username, DbType.String);
                parameters.Add("@Password", request.Password, DbType.String);
                parameters.Add("@LoginIP", request.LoginIP ?? (object)DBNull.Value, DbType.String);
                parameters.Add("@LoginType", request.LoginType ?? "System", DbType.String);

                // Execute stored procedure
                var result = await connection.QueryAsync<sp_UserLogin>(
                    "sp_UserLogin",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var user = result.FirstOrDefault();

                if (user == null)
                    return Result<sp_UserLogin>.Failure("User not found.");

                return Result<sp_UserLogin>.Success(user, "Login successful.");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in UserLogin - Number: {ErrorNumber}, Message: {Message}",
                    sqlEx.Number, sqlEx.Message);

                if (sqlEx.Number == 18456) // Login failed
                {
                    _logger.LogError("Login failed error - Check SQL Server Authentication Mode and 'sa' account status");
                    return Result<sp_UserLogin>.Failure("Database authentication failed. Please check server configuration.");
                }

                return Result<sp_UserLogin>.Failure($"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                Log.Information($"Error in getting User: {ex.Message}");
                _logger.LogError(ex, "Error in UserLogin");
                return Result<sp_UserLogin>.Failure("Error in getting User");
            }
        }

        public async Task<Result> SaveRefreshToken(int userId, string accessToken, string refreshToken, DateTime tokenExpiry, string? providerName, string? providerUserId)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                // Only open if closed - prevents issues with already open connections
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync();

                // Check if token already exists for this user/provider
                var existingToken = await connection.QueryFirstOrDefaultAsync<UserAuthProviderModel>(
                    "SELECT * FROM UserAuthProviders WHERE UserID = @UserID AND ProviderName = @ProviderName AND Del_Flg = 'N'",
                    new { UserID = userId, ProviderName = providerName }
                );

                if (existingToken != null)
                {
                    // Update existing token
                    var updateSql = @"
                        UPDATE UserAuthProviders
                        SET RefreshToken = @RefreshToken,
                            TokenExpiry = @TokenExpiry,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE ProviderID = @ProviderID";

                    var updateParams = new
                    {
                        RefreshToken = refreshToken,
                        TokenExpiry = tokenExpiry,
                        ModifiedDate = DateTime.UtcNow,
                        ModifiedBy = "System",
                        ProviderID = existingToken.ProviderID
                    };

                    var rowsUpdated = await connection.ExecuteAsync(updateSql, updateParams);
                    return rowsUpdated > 0
                        ? Result.Success("Refresh token updated successfully.")
                        : Result.Failure("Failed to update refresh token.");
                }

                // Insert new token
                var insertSql = @"
                    INSERT INTO UserAuthProviders
                    (UserID, ProviderName, ProviderUserID,AccessToken, RefreshToken, TokenExpiry, LinkedDate, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy, Del_Flg)
                    VALUES
                    (@UserID, @ProviderName, @ProviderUserID,@AccessToken, @RefreshToken, @TokenExpiry, @LinkedDate, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy, @Del_Flg)";

                var insertParams = new
                {
                    UserID = userId,
                    ProviderName = providerName,
                    ProviderUserID = providerUserId ?? Guid.NewGuid().ToString(),
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiry = tokenExpiry,
                    LinkedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System",
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedBy = "System",
                    Del_Flg = "N"
                };

                var rowsInserted = await connection.ExecuteAsync(insertSql, insertParams);
                return rowsInserted > 0
                    ? Result.Success("Refresh token saved successfully.")
                    : Result.Failure("Failed to save refresh token.");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in SaveRefreshToken - Number: {ErrorNumber}", sqlEx.Number);
                return Result.Failure($"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveRefreshToken");
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result<UserAuthProviderModel>> GetByRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var sql = @"SELECT * FROM UserAuthProviders 
                    WHERE RefreshToken = @RefreshToken AND Del_Flg != 'Y'";

                var token = await connection.QueryFirstOrDefaultAsync<UserAuthProviderModel>(
                    sql, new { RefreshToken = refreshToken });

                return token != null
                    ? Result<UserAuthProviderModel>.Success(token)
                    : Result<UserAuthProviderModel>.Failure("Token not found");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in GetByRefreshTokenAsync - Number: {ErrorNumber}", sqlEx.Number);
                return Result<UserAuthProviderModel>.Failure($"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByRefreshTokenAsync");
                return Result<UserAuthProviderModel>.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result<sp_UserLogin>> GetUserByIdAsync(int userId)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        U.UserID,
                        U.UserName,
                        U.Email,
                        U.MobileNumber,
                        U.UserTypeId,
                        UT.UserTypeName,
                        E.EmployeeID,
                        E.EmployeeCode,
                        E.FirstName,
                        E.LastName,
                        U.IsTwoFactorEnabled,
                        U.AccountLocked
                    FROM Users U
                    LEFT JOIN UserType UT ON U.UserTypeId = UT.UserTypeId
                    LEFT JOIN Employees E ON U.UserID = E.UserID
                    WHERE U.UserID = @UserID";

                var user = await connection.QueryFirstOrDefaultAsync<sp_UserLogin>(
                    sql, new { UserID = userId });

                return user != null
                    ? Result<sp_UserLogin>.Success(user)
                    : Result<sp_UserLogin>.Failure("User not found");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in GetUserByIdAsync - Number: {ErrorNumber}", sqlEx.Number);
                return Result<sp_UserLogin>.Failure($"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserByIdAsync");
                return Result<sp_UserLogin>.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result> UpdateRefreshTokenAsync(int providerId, string accessToken, string refreshToken, DateTime expiry, string modifiedBy)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var sql = @"
                    UPDATE UserAuthProviders
                    SET AccessToken = @AccessToken,
                        RefreshToken = @RefreshToken,
                        TokenExpiry = @TokenExpiry,
                        ModifiedDate = @ModifiedDate,
                        ModifiedBy = @ModifiedBy
                    WHERE ProviderID = @ProviderID";

                var rows = await connection.ExecuteAsync(sql, new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiry = expiry,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedBy = modifiedBy,
                    ProviderID = providerId
                });

                return rows > 0
                    ? Result.Success("Token updated successfully")
                    : Result.Failure("Failed to update token");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in UpdateRefreshTokenAsync - Number: {ErrorNumber}", sqlEx.Number);
                return Result.Failure($"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateRefreshTokenAsync");
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        // Reset Password Related Methods

        public async Task<Result<bool>> CreatePasswordResetTokenAsync(int userId, string? resetToken, DateTime? expiresAt, string? createdBy = null)
        {
            if (userId <= 0)
                return Result<bool>.Failure("Invalid user ID");

            if (string.IsNullOrWhiteSpace(resetToken))
                return Result<bool>.Failure("Reset token is required");

            if (expiresAt <= DateTime.UtcNow)
                return Result<bool>.Failure("Expiration date must be in the future");

            using var connection = _context.Database.GetDbConnection();

            try
            {
               
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var affectedRows = await connection.ExecuteAsync(
                    "sp_CreatePasswordResetToken",
                    new
                    {
                        UserId = userId,
                        ResetToken = resetToken,
                        ExpiresAt = expiresAt,
                        CreatedBy = createdBy ?? "System"
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                if (affectedRows > 0)
                {
                    _logger.LogInformation("Password reset token created for user: {UserId}", userId);
                    return Result<bool>.Success(true, "Token created successfully");
                }

                _logger.LogWarning("Failed to create password reset token for user: {UserId}", userId);
                return Result<bool>.Failure("Failed to create reset token");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in CreatePasswordResetTokenAsync - UserId: {UserId}, Error: {ErrorNumber}",
                    userId, sqlEx.Number);
                return Result<bool>.Failure("Database error occurred");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreatePasswordResetTokenAsync for user: {UserId}", userId);
                return Result<bool>.Failure("An unexpected error occurred");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<Result<UserModel>> GetUserByEmailAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result<UserModel>.Failure("Email is required");
            using var connection = _context.Database.GetDbConnection();
            try
            {

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var user = await connection.QueryFirstOrDefaultAsync<UserModel>(
                     @"SELECT TOP 1 
                 UserID, UserTypeId, UserName, Email, MobileNumber, PasswordHash,
                 IsEmailVerified, IsMobileVerified, IsTwoFactorEnabled,
                 Status, Del_Flg, CreatedDate, CreatedBy,
                 ModifiedDate, ModifiedBy, LastLoginDate, LoggedIn,
                 AccountLocked, LoginFailureAttempt, AccountStatusID,
                 AccountLockedDate
             FROM [dbo].[Users] WITH (NOLOCK)
             WHERE Email = @Email 
                 AND Del_Flg = 'N' 
                 AND Status = 'Active'",
                     new { Email = email }
                );

                if (user == null)
                {
                    _logger.LogInformation("User not found for email: {Email}", email);
                    return Result<UserModel>.Failure("Email not registered");
                }

                _logger.LogInformation("User retrieved successfully for email: {Email}", email);
                return Result<UserModel>.Success(user, "Email registered.");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in GetUserByEmailAsync - Number: {ErrorNumber}, Message: {Message}",
                    sqlEx.Number, sqlEx.Message);

                return sqlEx.Number switch
                {
                    -1 => Result<UserModel>.Failure("Database connection timeout"),
                    -2 => Result<UserModel>.Failure("Database connection failed"),
                    _ => Result<UserModel>.Failure("Database error occurred")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetUserByEmailAsync for email: {Email}", email);
                return Result<UserModel>.Failure("An unexpected error occurred");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<Result<PasswordResetToken?>> GetValidResetTokenAsync(string? resetToken)
        {
            if (string.IsNullOrWhiteSpace(resetToken))
                return Result<PasswordResetToken>.Failure("Reset token is required");

            try
            {
                using var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var token = await connection.QueryFirstOrDefaultAsync<PasswordResetToken>(
                    @"SELECT TOP 1 
                TokenID, UserId, ResetToken, ExpiresAt, IsUsed, CreatedAt, CreatedBy
            FROM [dbo].[PasswordResetTokens] WITH (NOLOCK)
            WHERE ResetToken = @ResetToken 
                AND IsUsed = 0 
                AND ExpiresAt > SYSUTCDATETIME()",
                    new { ResetToken = resetToken },
                    commandTimeout: 30
                );

                if (token == null)
                {
                    _logger.LogWarning("Invalid or expired reset token attempted");
                    return Result<PasswordResetToken>.Failure("Invalid or expired reset token");
                }

                _logger.LogInformation("Valid reset token found for user: {UserId}", token.UserId);
                return Result<PasswordResetToken>.Success(token, "Valid token found");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in GetValidResetTokenAsync - Error: {ErrorNumber}", sqlEx.Number);
                return Result<PasswordResetToken>.Failure("Database error occurred");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetValidResetTokenAsync");
                return Result<PasswordResetToken>.Failure("An unexpected error occurred");
            }
        }

        public async Task<Result<bool>> MarkResetTokenUsedAsync(string? resetToken)
        {
            if (string.IsNullOrWhiteSpace(resetToken))
                return Result<bool>.Failure("Reset token is required");

            try
            {
                using var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var affectedRows = await connection.ExecuteAsync(
                    @"UPDATE [dbo].[PasswordResetTokens] 
            SET IsUsed = 1, 
                UsedDate = SYSUTCDATETIME() 
            WHERE ResetToken = @ResetToken 
                AND IsUsed = 0",
                    new { ResetToken = resetToken },
                    commandTimeout: 30
                );

                if (affectedRows > 0)
                {
                    _logger.LogInformation("Reset token marked as used");
                    return Result<bool>.Success(true, "Token marked as used");
                }

                _logger.LogWarning("No token found to mark as used");
                return Result<bool>.Failure("Token not found or already used");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in MarkResetTokenUsedAsync - Error: {ErrorNumber}", sqlEx.Number);
                return Result<bool>.Failure("Database error occurred");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkResetTokenUsedAsync");
                return Result<bool>.Failure("An unexpected error occurred");
            }
        }

        public async Task<Result<bool>> ResetUserPasswordAsync(int userId, string? newPasswordHash)
        {
            if (userId <= 0)
                return Result<bool>.Failure("Invalid user ID");

            if (string.IsNullOrWhiteSpace(newPasswordHash))
                return Result<bool>.Failure("Password hash is required");

            try
            {
                using var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var affectedRows = await connection.ExecuteAsync(
                    @"UPDATE [dbo].[Users] 
            SET PasswordHash = @NewPasswordHash, 
                ModifiedDate = SYSUTCDATETIME(),
                ModifiedBy = 'PasswordReset',
                LoginFailureAttempt = 0,
                AccountLocked = 0,
                AccountLockedDate = NULL
            WHERE UserID = @UserId 
                AND Del_Flg = 'N'",
                    new { UserId = userId, NewPasswordHash = newPasswordHash },
                    commandTimeout: 30
                );

                if (affectedRows > 0)
                {
                    _logger.LogInformation("Password reset successfully for user: {UserId}", userId);
                    return Result<bool>.Success(true, "Password reset successfully");
                }

                _logger.LogWarning("User not found or inactive for password reset: {UserId}", userId);
                return Result<bool>.Failure("User not found or inactive");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in ResetUserPasswordAsync - UserId: {UserId}, Error: {ErrorNumber}",
                    userId, sqlEx.Number);
                return Result<bool>.Failure("Database error occurred");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetUserPasswordAsync for user: {UserId}", userId);
                return Result<bool>.Failure("An unexpected error occurred");
            }
        }

        public async Task<Result<bool>> CompletePasswordResetAsync(string resetToken, string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(resetToken))
                return Result<bool>.Failure("Reset token is required");

            if (string.IsNullOrWhiteSpace(newPasswordHash))
                return Result<bool>.Failure("Password hash is required");

            try
            {
                using var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Validate and get token
                    var token = await connection.QueryFirstOrDefaultAsync<PasswordResetToken>(
                        @"SELECT TOP 1 
                    TokenID, UserId, ResetToken, ExpiresAt, IsUsed, CreatedAt, CreatedBy
                FROM [dbo].[PasswordResetTokens] WITH (NOLOCK)
                WHERE ResetToken = @ResetToken 
                    AND IsUsed = 0 
                    AND ExpiresAt > SYSUTCDATETIME()",
                        new { ResetToken = resetToken },
                        transaction: transaction,
                        commandTimeout: 30
                    );

                    if (token == null)
                    {
                        transaction.Rollback();
                        _logger.LogWarning("Invalid or expired reset token attempted");
                        return Result<bool>.Failure("Invalid or expired reset token");
                    }

                    // 2. Update password
                    var affectedRows = await connection.ExecuteAsync(
                        @"UPDATE [dbo].[Users] 
                SET PasswordHash = @NewPasswordHash, 
                    ModifiedDate = SYSUTCDATETIME(),
                    ModifiedBy = 'PasswordReset',
                    LoginFailureAttempt = 0,
                    AccountLocked = 0,
                    AccountLockedDate = NULL
                WHERE UserID = @UserId 
                    AND Del_Flg = 'N'",
                        new { UserId = token.UserId, NewPasswordHash = newPasswordHash },
                        transaction: transaction,
                        commandTimeout: 30
                    );

                    if (affectedRows == 0)
                    {
                        transaction.Rollback();
                        _logger.LogWarning("User not found or inactive for password reset: {UserId}", token.UserId);
                        return Result<bool>.Failure("User not found or inactive");
                    }

                    // 3. Mark token as used
                    var tokenUpdated = await connection.ExecuteAsync(
                        @"UPDATE [dbo].[PasswordResetTokens] 
                SET IsUsed = 1, 
                    UsedDate = SYSUTCDATETIME() 
                WHERE ResetToken = @ResetToken",
                        new { ResetToken = resetToken },
                        transaction: transaction,
                        commandTimeout: 30
                    );

                    if (tokenUpdated == 0)
                    {
                        transaction.Rollback();
                        _logger.LogWarning("Failed to mark token as used");
                        return Result<bool>.Failure("Failed to mark token as used");
                    }

                    transaction.Commit();
                    _logger.LogInformation("Password reset completed successfully for user: {UserId}", token.UserId);
                    return Result<bool>.Success(true, "Password reset completed successfully");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in CompletePasswordResetAsync - Error: {ErrorNumber}", sqlEx.Number);
                return Result<bool>.Failure("Database error occurred");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompletePasswordResetAsync");
                return Result<bool>.Failure("An unexpected error occurred during password reset");
            }
        }
    }
}