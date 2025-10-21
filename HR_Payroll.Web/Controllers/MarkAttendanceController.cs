using HR_Payroll.Core.Model;
using HR_Payroll.Core.Response;
using HR_Payroll.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuGet.Protocol.Core.Types;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace HR_Payroll.Web.Controllers
{
    public class MarkAttendanceController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MarkAttendanceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly AuthCookieService _authCookieService;
        public MarkAttendanceController(IHttpClientFactory httpClientFactory,
            ILogger<MarkAttendanceController> logger,
            AuthCookieService authCookieService,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _authCookieService = authCookieService;
            _configuration = configuration;
        }
        // Read API base URL from appsettings.json
        public string _apiBaseUrl => _configuration.GetValue<string>("ApiBaseUrl");

        #region------------------- Attendance Marking -------------------

        public async Task<IActionResult> GetCurrentStatus()
        {
            try
            {
                var tokenValue = HttpContext.User.Claims
                   .FirstOrDefault(c => c.Type == "access_token")?.Value;
                var refreshToken = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "refresh_token")?.Value;

                if (string.IsNullOrEmpty(tokenValue) || string.IsNullOrEmpty(refreshToken))
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                var client = _httpClientFactory.CreateClient("AuthClient");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenValue);

                var response = await client.GetAsync($"{_apiBaseUrl}Attendance/GetAttendanceStatus");

                // 🔁 If token expired, try refreshing
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Access token expired. Attempting refresh...");

                    var refreshModel = new RefreshRequest
                    {
                        RefreshToken = refreshToken
                    };

                    var refreshJson = JsonConvert.SerializeObject(refreshModel);
                    var refreshContent = new StringContent(refreshJson, Encoding.UTF8, "application/json");

                    var refreshResponse = await client.PostAsync($"{_apiBaseUrl}Auth/RefreshToken", refreshContent);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var newTokens = JsonConvert.DeserializeObject<TokenResponse>(
                            await refreshResponse.Content.ReadAsStringAsync());

                        // 🔐 Re-sign user with new tokens
                        await _authCookieService.SignInUserWithJwt(newTokens.AccessToken, newTokens.RefreshToken, true);

                        // ✅ Retry original request with new token
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
                        response = await client.GetAsync($"{_apiBaseUrl}Attendance/GetAttendanceStatus");
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed. Logging out user.");
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("Login", "Account");
                    }
                }

                var responseData = await response.Content.ReadAsStringAsync();
                return Content(responseData, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentStatus for employee {EmployeeId}");
                return Json(new
                {
                    status = false,
                    message = "Internal server error occurred."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCheckin([FromForm] CheckInRequestModel model)
        {
            try
            {
                var tokenValue = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "access_token")?.Value;
                var refreshToken = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "refresh_token")?.Value;

                if (string.IsNullOrEmpty(tokenValue) || string.IsNullOrEmpty(refreshToken))
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                var client = _httpClientFactory.CreateClient("AuthClient");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenValue);

                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}Attendance/CheckIn", content);

                // 🔁 If token expired, try refreshing
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Access token expired. Attempting refresh...");

                    var refreshModel = new RefreshRequest
                    {
                        RefreshToken = refreshToken
                    };

                    var refreshJson = JsonConvert.SerializeObject(refreshModel);
                    var refreshContent = new StringContent(refreshJson, Encoding.UTF8, "application/json");

                    var refreshResponse = await client.PostAsync($"{_apiBaseUrl}Auth/RefreshToken", refreshContent);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var newTokens = JsonConvert.DeserializeObject<TokenResponse>(
                            await refreshResponse.Content.ReadAsStringAsync());

                        // 🔐 Re-sign user with new tokens
                        await _authCookieService.SignInUserWithJwt(newTokens.AccessToken, newTokens.RefreshToken, true);

                        // ✅ Retry original request with new token
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);

                        // Recreate content because previous StringContent is already consumed
                        var retryContent = new StringContent(json, Encoding.UTF8, "application/json");
                        response = await client.PostAsync($"{_apiBaseUrl}Attendance/CheckIn", retryContent);
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed. Logging out user.");
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("Login", "Account");
                    }
                }

                var responseData = await response.Content.ReadAsStringAsync();
                return Content(responseData, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Check-in failed");
                return Json(new
                {
                    status = false,
                    message = "Error while processing check-in"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCheckout([FromForm] CheckOutRequestModel model)
        {
            try
            {
                var tokenValue = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "access_token")?.Value;
                var refreshToken = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "refresh_token")?.Value;

                if (string.IsNullOrEmpty(tokenValue) || string.IsNullOrEmpty(refreshToken))
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                var client = _httpClientFactory.CreateClient("AuthClient");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenValue);

                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}Attendance/CheckOut", content);

                // 🔁 If token expired, try refreshing
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Access token expired. Attempting refresh...");

                    var refreshModel = new RefreshRequest
                    {
                        RefreshToken = refreshToken
                    };

                    var refreshJson = JsonConvert.SerializeObject(refreshModel);
                    var refreshContent = new StringContent(refreshJson, Encoding.UTF8, "application/json");

                    var refreshResponse = await client.PostAsync($"{_apiBaseUrl}Auth/RefreshToken", refreshContent);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var newTokens = JsonConvert.DeserializeObject<TokenResponse>(
                            await refreshResponse.Content.ReadAsStringAsync());

                        // 🔐 Re-sign user with new tokens
                        await _authCookieService.SignInUserWithJwt(newTokens.AccessToken, newTokens.RefreshToken, true);

                        // ✅ Retry original request with new token
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);

                        // Recreate content because previous StringContent is already consumed
                        var retryContent = new StringContent(json, Encoding.UTF8, "application/json");
                        response = await client.PostAsync($"{_apiBaseUrl}Attendance/CheckOut", retryContent);
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed. Logging out user.");
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("Login", "Account");
                    }
                }

                var responseData = await response.Content.ReadAsStringAsync();
                return Content(responseData, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Check-out failed");
                return Json(new
                {
                    status = false,
                    message = "Error while processing check-out"
                });
            }
        }

        #endregion

        #region------------------- Attendance Calender/ Data View -------------------

        public IActionResult AttendanceCalender()
        {
            return View();
        }

        public async Task<IActionResult> GetAttendanceCalendar(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var tokenValue = HttpContext.User.Claims
                   .FirstOrDefault(c => c.Type == "access_token")?.Value;
                var refreshToken = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "refresh_token")?.Value;

                if (string.IsNullOrEmpty(tokenValue) || string.IsNullOrEmpty(refreshToken))
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                var client = _httpClientFactory.CreateClient("AuthClient");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenValue);
                var queryParams = $"?FromDate={(fromDate.HasValue ? fromDate.Value.ToString("yyyy-MM-dd") : null)}" +
                  $"&ToDate={(toDate.HasValue ? toDate.Value.ToString("yyyy-MM-dd") : null)}";

                var response = await client.GetAsync($"{_apiBaseUrl}Attendance/GetAttendanceCalendar{queryParams}");
                
                // 🔁 If token expired, try refreshing
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Access token expired. Attempting refresh...");

                    var refreshModel = new RefreshRequest
                    {
                        RefreshToken = refreshToken
                    };

                    var refreshJson = JsonConvert.SerializeObject(refreshModel);
                    var refreshContent = new StringContent(refreshJson, Encoding.UTF8, "application/json");

                    var refreshResponse = await client.PostAsync($"{_apiBaseUrl}Auth/RefreshToken", refreshContent);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var newTokens = JsonConvert.DeserializeObject<TokenResponse>(
                            await refreshResponse.Content.ReadAsStringAsync());

                        // 🔐 Re-sign user with new tokens
                        await _authCookieService.SignInUserWithJwt(newTokens.AccessToken, newTokens.RefreshToken, true);

                        // ✅ Retry original request with new token
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
                        response = await client.GetAsync($"{_apiBaseUrl}Attendance/GetAttendanceCalendar{queryParams}");
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed. Logging out user.");
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("Login", "Account");
                    }
                }

                var responseData = await response.Content.ReadAsStringAsync();
                return Content(responseData, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get Attendance for employee {EmployeeId}");
                return Json(new
                {
                    status = false,
                    message = "Internal server error occurred."
                });
            }
        }


        public IActionResult AttendanceData()
        {
            return View();
        }

        public async Task<IActionResult> GetAttendanceReport(DateTime? fromDate,DateTime? toDate, int length, int start)
        {
            try
            {
                if (start==0)
                {
                    start = 1;
                }
                var tokenValue = HttpContext.User.Claims
                   .FirstOrDefault(c => c.Type == "access_token")?.Value;
                var refreshToken = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "refresh_token")?.Value;

                if (string.IsNullOrEmpty(tokenValue) || string.IsNullOrEmpty(refreshToken))
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }

                var client = _httpClientFactory.CreateClient("AuthClient");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenValue);

                var queryParams = $"?FromDate={(fromDate.HasValue ? fromDate.Value.ToString("yyyy-MM-dd") : null)}" +
                  $"&ToDate={(toDate.HasValue ? toDate.Value.ToString("yyyy-MM-dd") : null)}" +
                  $"&Start={start}" +
                  $"&Length={length}";

                var response = await client.GetAsync($"{_apiBaseUrl}Attendance/GetAttendanceReport{queryParams}");

                // 🔁 If token expired, try refreshing
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Access token expired. Attempting refresh...");

                    var refreshModel = new RefreshRequest
                    {
                        RefreshToken = refreshToken
                    };

                    var refreshJson = JsonConvert.SerializeObject(refreshModel);
                    var refreshContent = new StringContent(refreshJson, Encoding.UTF8, "application/json");

                    var refreshResponse = await client.PostAsync($"{_apiBaseUrl}Auth/RefreshToken", refreshContent);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var newTokens = JsonConvert.DeserializeObject<TokenResponse>(
                            await refreshResponse.Content.ReadAsStringAsync());

                        // 🔐 Re-sign user with new tokens
                        await _authCookieService.SignInUserWithJwt(newTokens.AccessToken, newTokens.RefreshToken, true);

                        // ✅ Retry original request with new token
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
                        response = await client.GetAsync($"{_apiBaseUrl}Attendance/GetAttendanceReport{queryParams}");
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed. Logging out user.");
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("Login", "Account");
                    }
                }

                var responseData = await response.Content.ReadAsStringAsync();
                return Content(responseData, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get Attendance for employee {EmployeeId}");
                return Json(new
                {
                    status = false,
                    message = "Internal server error occurred."
                });
            }
        }


        #endregion
    }
}
