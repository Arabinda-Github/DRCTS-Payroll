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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Json(new
                {
                    success = true,
                    message = "Session expired. Please log in again.",
                    redirectUrl = Url.Action("AdminLogin", "Account")
                });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid login request." });
            }

            try
            {
                model.RememberMe = true;
                var httpClient = _httpClientFactory.CreateClient("AuthClient");
                var tokenEndpoint = $"{ApiEndPoint}Auth";
                var requestData = new { username = model.Username, password = model.Password };
                var requestContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var tokenResponse = await httpClient.PostAsync(tokenEndpoint, requestContent);
                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(tokenContent);
                if (tokenResponse.IsSuccessStatusCode && loginResponse.status)
                {
                    await SignInUserWithJwt(loginResponse.token ?? string.Empty, model.RememberMe);
                    _logger.LogInformation("User {Email} logged in at {Time}.",
                         "amit@gmail.com", DateTime.UtcNow);
                    //return RedirectToAction("Dashboard", "Home");
                    return Json(new
                    {
                        success = true,
                        message = "Logging in please wait......",
                        redirectUrl = Url.Action("Dashboard", "Admin")
                    });
                }
                else
                {
                    return Json(new { success = false, message = loginResponse.message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return Json(new { success = false, message = "An error occurred during login" });
            }
        }

        private async Task SignInUserWithJwt(string token, bool rememberMe)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = tokenHandler.ReadJwtToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid JWT token format.");
                throw new SecurityTokenException("Invalid token.");
            }

            // ✅ Use claims directly from token
            var claims = jwtToken.Claims.ToList();
            claims.Add(new Claim("access_token", token));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = jwtToken.ValidTo // ✅ Sync cookie lifetime with JWT expiry
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
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
