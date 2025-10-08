using HR_Payroll.API.Config;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HR_Payroll.API.JWTExtension
{
    public class JWTServiceExtension
    {
        private readonly IConfiguration _configuration;
        private readonly JwtIdentitySetting _serverSettings;
        public JWTServiceExtension(
           IConfiguration configuration, JwtIdentitySetting serverSettings)
        {
            _serverSettings = serverSettings;
            _configuration = configuration;
        }
        //public string GenerateJwtToken(sp_GetUserDetail user)
        public string GenerateJwtToken()
        {
            //if (user == null)
            //    throw new ArgumentNullException(nameof(user));

            // Retrieve JWT configuration settings
            var secretKey = _configuration["JwtIdentitySetting:Secret"]
                            ?? throw new InvalidOperationException("JWT secret key not configured.");

            var issuer = _serverSettings.Issuer;
            var audience = _serverSettings.Audience;
            var expiryMinutes = _serverSettings.Expiry;

            // Create security credentials
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256
            );

            // Build claims
            var claims = new[]
            {
                new Claim(ClaimTypes.SerialNumber, Guid.NewGuid().ToString())
                //new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),        
                //new Claim("Username", user.EmailId ?? user.MobileNo ?? string.Empty),
            };

            // Create the JWT token
            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            // Return the token string
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

    }
}
