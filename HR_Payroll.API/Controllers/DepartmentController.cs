using HR_Payroll.Core.Response;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly ILogger<DepartmentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDepartmentServices _deptRepository;

        public DepartmentController(IConfiguration configuration,
            ILogger<DepartmentController> logger,
            IDepartmentServices deptRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _deptRepository = deptRepository;

        }

        [HttpGet]
        [Route("GetAllDepartments")]
        public async Task<ActionResult> GetAllDepartments()
        {
            try
            {
                var result = await _deptRepository.GetDepartmentsAsync();

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve department list",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Department list retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department list");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new List<object>()
                });
            }
        }
    }
}
