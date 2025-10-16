using HR_Payroll.Core.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace HR_Payroll.Web.Controllers
{
    public class MarkAttendanceController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MarkAttendanceController> _logger;
        private readonly IConfiguration _configuration;
        public MarkAttendanceController(IHttpClientFactory httpClientFactory,
            ILogger<MarkAttendanceController> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }
        // Read API base URL from appsettings.json
        public string _apiBaseUrl => _configuration.GetValue<string>("ApiBaseUrl");

        [HttpPost]
        public async Task<IActionResult> ProcessCheckin([FromForm] CheckInRequestModel model)
        {
            if (model == null || model.EmployeeID <= 0)
            {
                return Json(new
                {
                    isSuccess = false,
                    message = "Invalid Employee ID"
                });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("AuthClient");
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}Attendance/CheckIn", content);
                var responseData = await response.Content.ReadAsStringAsync();

                return Content(responseData, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Check-in failed");
                return Json(new
                {
                    isSuccess = false,
                    message = "Error while processing check-in"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCheckout([FromForm] CheckOutRequestModel model)
        {
            if (model == null || model.EmployeeID <= 0)
            {
                return Json(new
                {
                    isSuccess = false,
                    message = "Invalid Employee ID"
                });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("AuthClient");
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}Attendance/CheckOut", content);
                var responseData = await response.Content.ReadAsStringAsync();

                return Content(responseData, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Check-out failed");
                return Json(new
                {
                    isSuccess = false,
                    message = "Error while processing check-out"
                });
            }
        }
    }
}
