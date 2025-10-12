using Dapper;
using HR_Payroll.Core.Entity;
using HR_Payroll.Core.Model;
using HR_Payroll.Core.Services;
using HR_Payroll.Infrastructure.Data;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Data;

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

                if (connection.State != ConnectionState.Open)
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
                    (UserID, ProviderName, ProviderUserID, RefreshToken, TokenExpiry, LinkedDate, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy, Del_Flg)
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
    }
}