using HR_Payroll.API.Config;
using HR_Payroll.Core.Entity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
        public string GenerateJwtToken(sp_UserLogin user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

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
            var name = user.FirstName + ' ' + user.LastName;
            // Build claims
            var claims = new[]
            {
                new Claim(ClaimTypes.SerialNumber, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.PrimarySid, user.UserID.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserTypeId.ToString()),
                new Claim(ClaimTypes.Role, user.UserTypeName!.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, user.Email!.ToString()),
                new Claim(ClaimTypes.MobilePhone, user.MobileNumber!.ToString()),
                new Claim("Department", user.Department ?? string.Empty),
                new Claim("Designation", user.Designation ?? string.Empty),
                new Claim("EmployeeId", user.EmployeeID.ToString() ?? string.Empty),
                new Claim("EmpCode", user.EmployeeCode ?? string.Empty),
                new Claim("ProfilePic", user.ProfilePic ?? string.Empty)
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

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        // =========================================
        // Helper: This method is used only when your access token(JWT) has already expired,
        //         and you need to extract user identity(claims like username, user ID, etc.)
        //         from that expired token to issue a new token.
        // =========================================

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtIdentitySetting:Secret"])),
                ValidateLifetime = false // we are validating expired tokens here
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

    }
}
