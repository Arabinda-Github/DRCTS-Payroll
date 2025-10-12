using HR_Payroll.Core.Entity;
using HR_Payroll.Core.Model;
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
    }
}
