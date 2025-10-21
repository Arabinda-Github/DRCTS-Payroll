using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace HR_Payroll.Core.Services
{ 
    public class AuthCookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthCookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SignInUserWithJwt(string accessToken, string refreshToken, bool rememberMe)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);
            var claims = jwtToken.Claims.ToList();

            // Add both tokens as claims
            claims.Add(new Claim("access_token", accessToken));
            claims.Add(new Claim("refresh_token", refreshToken));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = jwtToken.ValidTo,
                AllowRefresh = true
            };

            var httpContext = _httpContextAccessor.HttpContext!;
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        }

        public async Task SignOutUser()
        {
            var httpContext = _httpContextAccessor.HttpContext!;
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

}
