using HR_Payroll.API.JWTExtension;
using HR_Payroll.Infrastructure.Concrete;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Identity;

namespace HR_Payroll.API
{
    public static class ServiceExtension
    {
        public static void ConfigureDIServices(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddScoped<JWTServiceExtension>();           
            services.AddScoped<IPasswordHasher, PasswordHasher>();          
        }
    }
}
