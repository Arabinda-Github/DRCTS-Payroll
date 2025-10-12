using HR_Payroll.Core.Model;
using HR_Payroll.Core.Response;
using HR_Payroll.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
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
        // =========================================
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
            // Parse JWT to get claims
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);

            var claims = jwtToken.Claims.ToList();

            // Add refresh token as claim for easy access
            claims.Add(new Claim("RefreshToken", refreshToken));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = jwtToken.ValidTo,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties
            );
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Home");
        }

        public IActionResult ResetPassword()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
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
