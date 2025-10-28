using HR_Payroll.Core.Entity;
using HR_Payroll.Core.Model.Auth;
using HR_Payroll.Core.Services;


namespace HR_Payroll.Infrastructure.Interface
{
    public interface IAuthService
    {
        Task<Result<sp_UserLogin>> UserLoginAsync(LoginModel request);
        Task<Result> SaveRefreshToken(int userId,string accessToken, string refreshToken, DateTime tokenExpiry, string? providerName, string? providerUserId);
        Task<Result<UserAuthProviderModel>> GetByRefreshTokenAsync(string refreshToken);
        Task<Result<sp_UserLogin>> GetUserByIdAsync(int userId);
        Task<Result> UpdateRefreshTokenAsync(int providerId, string accessToken, string refreshToken, DateTime expiry, string modifiedBy);

        // Reset Password Related Methods
        Task<Result<UserModel>> GetUserByEmailAsync(string? email);
        Task<Result<bool>> CreatePasswordResetTokenAsync(int userId, string? resetToken, DateTime? expiresAt, string? createdBy = null);
        Task<Result<PasswordResetToken?>> GetValidResetTokenAsync(string? resetToken);
        Task<Result<bool>> MarkResetTokenUsedAsync(string? resetToken);
        Task<Result<bool>> ResetUserPasswordAsync(int userId, string? newPasswordHash);
    }
}
