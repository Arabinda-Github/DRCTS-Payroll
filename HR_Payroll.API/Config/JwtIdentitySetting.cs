using Microsoft.AspNetCore.Identity;

namespace HR_Payroll.API.Config
{
    public class JwtIdentitySetting
    {
        public string[]? AllowedOrigins { get; set; }
        public string? ApiName { get; set; }
        public string? Secret { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public double Expiry { get; set; }

        public static class IdentityServerConfigurations
        {
            internal static Action<IdentityOptions> GetConfigureIdentityOptions()
            {
                return options =>
                {
                    // password requirements
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = false;

                    // lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;

                    // user validation settings
                    options.User.RequireUniqueEmail = true;

                    // sign-in settings
                    options.SignIn.RequireConfirmedEmail = true;
                };
            }
        }
    }
}
