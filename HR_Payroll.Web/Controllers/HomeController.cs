using HR_Payroll.Core.Model.Auth;
using HR_Payroll.Core.Response;
using HR_Payroll.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HR_Payroll.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(IHttpClientFactory httpClientFactory,ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }
        public string ApiEndPoint => _configuration.GetValue<string>("ApiBaseUrl");

        public async Task<IActionResult> Login()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            string username = string.Empty;
            string password = string.Empty;
            bool rememberMe = false;
            // Read cookie from request
            if (Request.Cookies.TryGetValue("RememberMe", out string encryptedData))
            {
                try
                {
                    // Decode base64 string back to username|password
                    var decodedBytes = Convert.FromBase64String(encryptedData);
                    var decodedText = Encoding.UTF8.GetString(decodedBytes);

                    // Split the text into username and password
                    var parts = decodedText.Split('|');
                    if (parts.Length == 2)
                    {
                        username = parts[0];
                        password = parts[1];
                    }
                    rememberMe = true;
                }
                catch (FormatException ex)
                {
                    // Log any invalid cookie format
                    Console.WriteLine($"Invalid RememberMe cookie: {ex.Message}");
                }
            }

            // Pass to the view model so Razor can prefill fields
            var model = new LoginModel
            {
                Username = username,
                Password = password,
                RememberMe = rememberMe
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid login request." });

            try
            {
                var httpClient = _httpClientFactory.CreateClient("AuthClient");
                var tokenEndpoint = $"{ApiEndPoint}Auth";

                // Use System.Text.Json for better performance
                var requestContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(model),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync(tokenEndpoint, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API returned {StatusCode} for user {User}", response.StatusCode, model.Username);
                    return Json(new { success = false, message = "Authentication service unavailable." });
                }

                var content = await response.Content.ReadAsStringAsync();
                var loginResponse = System.Text.Json.JsonSerializer.Deserialize<LoginResponse<TokenData>>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (loginResponse?.status == true && loginResponse.data != null)
                {
                    var tokenData = loginResponse.data;

                    // Sign in with JWT
                    await SignInUserWithJwt(tokenData.accessToken, tokenData.refreshToken, model.RememberMe);

                    // Handle RememberMe cookie
                    if (model.RememberMe)
                    {
                        SetRememberMeCookie(model.Username, model.Password);
                    }
                    else
                    {
                        Response.Cookies.Delete("RememberMe");
                    }

                    _logger.LogInformation("User {User} logged in successfully", model.Username);

                    return Json(new
                    {
                        success = true,
                        message = "Login successful",
                        redirectUrl = Url.Action("AdminDashboard", "Dashboard")
                    });
                }

                return Json(new { success = false, message = loginResponse?.message ?? "Invalid credentials." });
            }
            catch (System.Text.Json.JsonException jex)
            {
                _logger.LogError(jex, "JSON deserialization error for user {User}", model.Username);
                return Json(new { success = false, message = "Invalid response from server." });
            }
            catch (HttpRequestException hex)
            {
                _logger.LogError(hex, "HTTP request failed for user {User}", model.Username);
                return Json(new { success = false, message = "Could not connect to authentication service." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for user {User}", model.Username);
                return Json(new { success = false, message = "An error occurred during login." });
            }
        }

        // =========================================
        // Helper: Sign-in using JWT remember and refresh token
        private void SetRememberMeCookie(string username, string password)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true
            };

            // Better encryption - consider using Data Protection API
            var encryptedData = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{username}|{password}")
            );

            Response.Cookies.Append("RememberMe", encryptedData, cookieOptions);
        }

        private async Task SignInUserWithJwt(string accessToken, string refreshToken, bool rememberMe)
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);
            var claims = jwtToken.Claims.ToList();

            // Add tokens as claims for reuse
            claims.Add(new Claim("access_token", accessToken));
            claims.Add(new Claim("refresh_token", refreshToken));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = jwtToken.ValidTo, // expire when JWT expires
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties
            );
        }

        // =========================================

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Home");
        }

        public IActionResult Profile()
        {
            return View();
        }

        public async Task<IActionResult> ForgotPassword()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string? Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
                return Json(new { success = false, message = "Please enter your registered email address." });

            try
            {
                var httpClient = _httpClientFactory.CreateClient("AuthClient");

                // API endpoint for forgot password
                var apiUrl = $"{ApiEndPoint}Auth/ForgotPassword";

                var model = new ForgotPasswordRequest
                {
                    Email = Email.Trim()
                };

                var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(model),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync(apiUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ForgotPassword API failed ({StatusCode}) for email {Email}", response.StatusCode, Email);
                    return Json(new { success = false, message = "Password reset request failed. Please try again later." });
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }
            catch (System.Text.Json.JsonException jex)
            {
                _logger.LogError(jex, "JSON error during ForgotPassword for {Email}", Email);
                return Json(new { success = false, message = "Invalid response received from server." });
            }
            catch (HttpRequestException hex)
            {
                _logger.LogError(hex, "HTTP error during ForgotPassword for {Email}", Email);
                return Json(new { success = false, message = "Unable to connect to authentication service." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ForgotPassword for {Email}", Email);
                return Json(new { success = false, message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ValidateToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return Json(new { success = false, message = "Invalid token." });

            try
            {
                var httpClient = _httpClientFactory.CreateClient("AuthClient");
                var response = await httpClient.GetAsync($"{ApiEndPoint}Auth/ValidateToken?Token={Uri.EscapeDataString(token)}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ValidateToken API returned {StatusCode} for token: {Token}", response.StatusCode, token);
                    return Json(new { success = false, message = "Token invalid, expired, or already used." });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ValidateToken for token: {Token}", token);
                return Json(new { success = false, message = "An unexpected error occurred. Please try again later." });
            }
        }

        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestModel model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            try
            {
                var httpClient = _httpClientFactory.CreateClient("AuthClient");
                var apiUrl = $"{ApiEndPoint}Auth/ResetPassword";

                var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(model),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync(apiUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ResetPassword API returned {StatusCode} for user", response.StatusCode);
                    return Json(new { success = false, message = "Password reset failed. Please try again later." });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ResetPassword for user");
                return Json(new { success = false, message = "An unexpected error occurred. Please try again later." });
            }
        }

        public async Task<IActionResult> AccessDenied()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();
        }

        [HttpGet]
        public IActionResult GetCookieValue()
        {
            var user = HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userid = user.FindFirst(ClaimTypes.PrimarySid)?.Value;
                var usertypeid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = user.FindFirst(ClaimTypes.Role)?.Value;
                var name = user.FindFirst(ClaimTypes.Name)?.Value;
                var email = user.FindFirst(ClaimTypes.Email)?.Value;
                var mobile = user.FindFirst(ClaimTypes.MobilePhone)?.Value;
                var profilpic = user.FindFirst("ProfilePic")?.Value;
                var empid = Convert.ToInt32(user.FindFirst("EmployeeId")?.Value);
                var empcode= user.FindFirst("EmpCode")?.Value;
                var dept = user.FindFirst("Department")?.Value;
                var design = user.FindFirst("Designation")?.Value;
                var userdetails = new
                {
                    userid = userid,
                    usertypeid= usertypeid,
                    role=role,
                    name = name,
                    email = email,
                    Mob = mobile,
                    pfile = profilpic,
                    empid= empid, 
                    empcode=empcode,
                    dept=dept,
                    design=design
                };

                return Json(new
                {
                    success = true,
                    data = userdetails
                });
            }

            return Json(new
            {
                success = false,
                data = ""
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
