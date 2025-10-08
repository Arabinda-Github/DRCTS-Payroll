using HR_Payroll.API.Config;
using HR_Payroll.API.JWTExtension;
using HR_Payroll.CommonCases.Utility;
using HR_Payroll.Core.Enum;
using HR_Payroll.Core.Model;
using HR_Payroll.Core.Response;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        private readonly IAuthService _iuserService;
        public AuthController(
             JwtIdentitySetting serverSettings,
             JWTServiceExtension jwtService,             
             IPasswordHasher passwordHasher,
             IConfiguration configuration,
             IAuthService userService)
        {
            _serverSettings = serverSettings;
            _configuration = configuration;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _iuserService = userService;
        }

        [HttpPost]
        public async Task<ActionResult<LoginResponse>> Post([FromBody] LoginModel request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return Ok(new LoginResponse
                    {
                        status = false,
                        message = "Username and password are required"
                    });
                }

                //var userResult = await _iuserService.GetUser(request);
                var userResult = new[]
                {
                    new { UserId = 1, FullName = "Arabinda Maharana", EmailId = "arabinda@example.com", MobileNo = "9876543210", UserType = "Admin", ProfilePic = "profile1.png", Password = "12345", IsActive = "Y" },
                    new { UserId = 2, FullName = "Ravi Kumar", EmailId = "ravi@example.com", MobileNo = "9123456789", UserType = "Manager", ProfilePic = "profile2.png", Password = "abcde", IsActive = "Y" },
                    new { UserId = 3, FullName = "Sneha Patra", EmailId = "sneha@example.com", MobileNo = "9988776655", UserType = "Employee", ProfilePic = "profile3.png", Password = "pass123", IsActive = "N" }
                };
                var user = userResult.FirstOrDefault();
                if (user == null)
                {
                    return Ok(new LoginResponse
                    {
                        status = false,
                        message = "User not found"
                    });
                }

                if (user.IsActive != "Y")
                {
                    return Ok(new LoginResponse
                    {
                        status = false,
                        message = "User is inactive or deleted"
                    });
                }

                if (!_passwordHasher.VerifyBase64Password(request.Password, user.Password))
                {
                    return Ok(new LoginResponse
                    {
                        status = false,
                        message = "Invalid password"
                    });
                }

                //var token = _jwtService.GenerateJwtToken(userResult);
                return Ok(new LoginResponse
                {
                    status = true,
                    message = "Login successful"
                    //token = token
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    status = false,
                    message = "An error occurred during login"
                });
            }
        }

        //[HttpPost]
        //[Route("GetUserDetails")]
        //public async Task<IActionResult> GetUserDetails([FromBody] LoginModel request)
        //{
        //    var result = await _iuserService.GetUser(request);

        //    if (result.IsSuccess)
        //    {
        //        var availability = result.Entity;
        //        bool hasData = availability != null && availability.Any();

        //        return Ok(new DataResponse<object>
        //        {
        //            status = hasData ? true : false,
        //            message = hasData ? "Data found" : "Data not found",
        //            data = hasData
        //                ? new List<object> { new { RoomDetails = availability } }
        //                : new List<object>()
        //        });
        //    }

        //    return Ok(new DataResponse<object>
        //    {
        //        status = false,
        //        message = result.Status == ResultStatusType.NotFound
        //            ? "Data not found"
        //            : result.Message ?? "Failed to get details",
        //        data = new List<object>()
        //    });
        //}

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
