using HR_Payroll.API.Config;
using HR_Payroll.API.JWTExtension;
using HR_Payroll.CommonCases.Utility;
using HR_Payroll.Core.Model.Auth;
using HR_Payroll.Core.Model.Email;
using HR_Payroll.Core.Response;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;


namespace HR_Payroll.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly JwtIdentitySetting _serverSettings;
        private readonly JWTServiceExtension _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly EmailConfiguration _emailConfig;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        public AuthController(
             JwtIdentitySetting serverSettings,
             JWTServiceExtension jwtService,             
             IPasswordHasher passwordHasher,
             IConfiguration configuration,
             IEmailService emailService,
             IAuthService authService,
             ILogger<AuthController> logger)
        {
            _serverSettings = serverSettings;
            _emailConfig = configuration.GetSection("EmailSettings").Get<EmailConfiguration>();
            _configuration = configuration;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<LoginResponse<TokenData>>> Post([FromBody] LoginModel request)
        {
            // Early validation
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Ok(new LoginResponse<TokenData>
                {
                    status = false,
                    message = "Username and password are required."
                });
            }

            try
            {
                // Encrypt password
                request.Password = ExternalHelper.Encrypt(request.Password);

                // Call service
                var loginResult = await _authService.UserLoginAsync(request);

                // Check failure - combined conditions
                if (!loginResult.IsSuccess || loginResult.Entity == null || !loginResult.Entity.Success)
                {
                    return Ok(new LoginResponse<TokenData>
                    {
                        status = false,
                        message = loginResult.Entity?.Message ?? loginResult.Message ?? "Invalid credentials."
                    });
                }

                var user = loginResult.Entity;

                // Generate tokens
                var jwtToken = _jwtService.GenerateJwtToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(60);

                // Store refresh token - AWAIT to ensure proper connection handling
                var saveResult = await _authService.SaveRefreshToken(
                    user.UserID,
                    jwtToken,
                    refreshToken,
                    expiresAt,
                    "System",
                    null
                );

                // Log warning if token save failed, but don't block login success
                if (!saveResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to save refresh token for user {UserId}: {Message}",
                        user.UserID, saveResult.Message);
                }

                return Ok(new LoginResponse<TokenData>
                {
                    status = true,
                    message = "Login successful",
                    data = new TokenData
                    {
                        accessToken = jwtToken,
                        refreshToken = refreshToken,
                        expiresAt = expiresAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user: {Username}", request.Username);
                return StatusCode(500, new LoginResponse<TokenData>
                {
                    status = false,
                    message = "An error occurred during login. Please try again."
                });
            }
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
        {
            // 1. Get existing refresh token from DB via service
            var existingTokenResult = await _authService.GetByRefreshTokenAsync(request.RefreshToken);

            if (!existingTokenResult.IsSuccess || existingTokenResult.Entity == null)
                return NotFound(new { status = false, message = "Invalid or expired refresh token" });

            var existingToken = existingTokenResult.Entity;

            // 2. Get user info
            var userResult = await _authService.GetUserByIdAsync(existingToken.UserID);

            if (!userResult.IsSuccess || userResult.Entity == null)
                return NotFound(new { status = false, message = "User not found" });

            var user = userResult.Entity;

            // 3. Generate new tokens in controller
            var newAccessToken = _jwtService.GenerateJwtToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // 4. Update DB via service
            var updateResult = await _authService.UpdateRefreshTokenAsync(existingToken.ProviderID, newAccessToken, newRefreshToken, DateTime.UtcNow.AddDays(30), user.UserName);

            if (!updateResult.IsSuccess)
                return BadRequest(new { status = false, message = updateResult.Message });

            // 5. Return new tokens
            return Ok(new
            {
                status = true,
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        [HttpPost]
        [Route("GetUserDetails")]
        public async Task<IActionResult> GetUserDetails([FromBody] LoginModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Ok(new DataResponse<object>
                {
                    status = false,
                    message = "Username is required",
                    data = new List<object>()
                });
            }

            try
            {
                var result = await _authService.UserLoginAsync(request);

                if (result.IsSuccess && result.Entity != null)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = true,
                        message = "Data found",
                        data = new List<object> { new { UserDetails = result.Entity } }
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = false,
                    message = result.Message ?? "Data not found",
                    data = new List<object>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching user details: {ex.Message}");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while fetching data",
                    data = new List<object>()
                });
            }
        }

        [HttpPost]
        [Route("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                return BadRequest(new DataResponse<object> 
                {
                    status = false, 
                    message = "Email is required",
                    data = new List<object>()
                });

            var email = model.Email.Trim();
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest(new DataResponse<object> 
                { 
                    status = false, 
                    message = "Invalid email format",
                    data = new List<object>()
                });

            var user = await _authService.GetUserByEmailAsync(email);
            if (user == null)
                return NotFound(new DataResponse<object> 
                { 
                    status = false, 
                    message = "Email not registered",
                    data = new List<object>()
                });

            // Create token & expiry
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                        .Replace("+", "")
                        .Replace("/", "")
                        .Replace("=", ""); // shorter safe token
            var expiresAt = DateTime.UtcNow.AddHours(1);

            await _authService.CreatePasswordResetTokenAsync(user.Entity.UserID, token, expiresAt, "System");

            // Generate reset link (point to front-end reset page)
            var resetLink = $"{Request.Scheme}://{Request.Host}/Home/ResetPassword?token={WebUtility.UrlEncode(token)}";

            // Send email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Entity.Email, email, resetLink, _emailConfig);
                return Ok(new DataResponse<object> 
                { 
                    status = true, 
                    message = "Password reset link sent to your email.",
                    data = new List<object>()
                });
            }
            catch (Exception ex)
            {
                // log ex
                return StatusCode(500, new DataResponse<object> 
                { 
                    status = false,
                    message = "Failed to send reset email.",
                    data = new List<object>()
                });
            }
        }

        [HttpGet]
        [Route("ValidateToken")]
        public async Task<IActionResult> ValidateToken([FromQuery] string Token)
        {
            if (string.IsNullOrWhiteSpace(Token))
                return BadRequest(new DataResponse<object>
                {
                    status = false,
                    message = "Token required",
                    data = new List<object>()
                });

            var tokenRow = await _authService.GetValidResetTokenAsync(Token);
            if (tokenRow == null)
                return NotFound(new DataResponse<object>
                {
                    status = false, 
                    message = "Token invalid or expired/used",
                    data = new List<object>()
                });

            return Ok(new DataResponse<object>
            {
                status = true, 
                message = "Token valid",
                data = new { tokenRow.Entity.UserId }
            });
        }

        // POST: api/Account/ResetPassword
        [HttpPost]
        [Route("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestModel req)
        {
            if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new DataResponse<object> 
                {
                    status = false, 
                    message = "Token and new password required", 
                    data = new List<object>()
                });

            // Validate token
            var tokenRow = await _authService.GetValidResetTokenAsync(req.Token);
            if (tokenRow == null)
                return NotFound(new DataResponse<object> 
                { 
                    status = false, 
                    message = "Token invalid or expired/used",
                    data = new List<object>() 
                });

            // Optional: Check whether the user's password was changed after token creation
            var fullUser = await _authService.GetUserByIdAsync(tokenRow.Entity.UserId);
            if (fullUser == null)
                return NotFound(new DataResponse<object> 
                { 
                    status = false, 
                    message = "User not found", 
                    data = new List<object>()
                });

            // Hash new password
            var newHash = ExternalHelper.Encrypt(req.NewPassword);

            // Update password
            await _authService.ResetUserPasswordAsync(fullUser.Entity.UserID, newHash);

            // Mark token used
            await _authService.MarkResetTokenUsedAsync(req.Token);

            return Ok(new DataResponse<object> 
            { 
                status = true, 
                message = "Password changed successfully",
                data = new List<object>() 
            });
        }

        [HttpGet]
        [Route("Encrypt/{password}")]
        public async Task<ActionResult<DataResponse<string>>> Encrypt(string password)
        {
            try
            {
                return Ok(new DataResponse<string>
                {
                    status = true,
                    message = "Password Encrypted Successfully",
                    data = ExternalHelper.Encrypt(password)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DataResponse<string>
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("Decrypt")]
        public async Task<ActionResult<DataResponse<string>>> Decrypt(string password)
        {
            try
            {
                return Ok(new DataResponse<string>
                {
                    status = true,
                    message = "Password Decrypted Successfully",
                    data = ExternalHelper.Decrypt(password)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DataResponse<string>
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("Claim")]
        public async Task<ActionResult<DataResponse<List<Claim>>>> Claim()
        {
            try
            {
                var claims = User.Claims.ToList();
                return Ok(new DataResponse<List<Claim>>
                {
                    status = true,
                    message = "Claim Retrived Successfully",
                    data = claims
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DataResponse<string>
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

    }
}
