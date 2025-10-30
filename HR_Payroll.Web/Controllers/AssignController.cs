using HR_Payroll.Core.DTO.Dept;
using HR_Payroll.Core.Model.Master;
using HR_Payroll.Core.Services;
using HR_Payroll.Web.CommonClients;
using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.Web.Controllers
{
    public class AssignController : Controller
    {
        private readonly CommonAPI_Client _apiClient;
        private readonly ILogger<AssignController> _logger;
        private readonly IConfiguration _configuration;

        public AssignController(CommonAPI_Client apiClient,
            ILogger<AssignController> logger,
            IConfiguration configuration)
        {
            _apiClient = apiClient;
            _logger = logger;
            _configuration = configuration;
        }
        public string _apiBaseUrl => _configuration.GetValue<string>("ApiBaseUrl");

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var accessToken = User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
                var refreshToken = User.Claims.FirstOrDefault(c => c.Type == "refresh_token")?.Value;

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        message = "Missing access or refresh token."
                    });
                }

                _apiClient.SetTokens(accessToken, refreshToken);

                var result = await _apiClient.GetAsync<List<DepartmentDTO>>("Department/GetAllDepartments");

                if (!result.status)
                {
                    _logger.LogWarning("Failed to retrieve departments: {Message}", result.message);
                    return StatusCode(500, new
                    {
                        status = false,
                        message = result.message
                    });
                }

                return Json(new
                {
                    status = true,
                    message = "Departments retrieved successfully.",
                    data = result.data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetDepartments");
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving departments."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubDepartments(int departmentId)
        {
            var accessToken = User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
            var refreshToken = User.Claims.FirstOrDefault(c => c.Type == "refresh_token")?.Value;

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { status = false, message = "Missing tokens." });
            }

            _apiClient.SetTokens(accessToken, refreshToken);

            var result = await _apiClient.GetAsync<List<SubDepartmentDTO>>(
                "SubDepartment/GetAllSubDepartments",
                new Dictionary<string, string> { { "departmentId", departmentId.ToString() } }
            );

            if (!result.status)
            {
                return StatusCode(500, new { status = false, message = result.message });
            }

            return Json(new
            {
                status = true,
                message = "Sub-departments loaded.",
                data = result.data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeBySubDept(int subDepartmentId)
        {
            var accessToken = User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
            var refreshToken = User.Claims.FirstOrDefault(c => c.Type == "refresh_token")?.Value;

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { status = false, message = "Missing tokens." });
            }

            _apiClient.SetTokens(accessToken, refreshToken);

            var result = await _apiClient.GetAsync<List<BranchWiseUserModel>>(
                "SubDepartment/GetEmployeeBySubDepartment",
                new Dictionary<string, string> { { "subDepartmentId", subDepartmentId.ToString() } }
            );

            if (!result.status)
            {
                return StatusCode(500, new { status = false, message = result.message });
            }

            return Json(new
            {
                status = true,
                message = "employee loaded.",
                data = result.data
            });
        }

        public IActionResult AssignEmployee() => View();
        public IActionResult AssignManager() => View();
        public IActionResult AssignTeamLeader() => View();

    }
}
