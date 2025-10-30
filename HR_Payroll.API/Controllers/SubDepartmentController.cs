using HR_Payroll.Core.Response;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubDepartmentController : ControllerBase
    {
        private readonly ILogger<SubDepartmentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISubDepartmentServices _subdeptRepository;

        public SubDepartmentController(IConfiguration configuration,
            ILogger<SubDepartmentController> logger,
            ISubDepartmentServices subdeptRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _subdeptRepository = subdeptRepository;

        }
        [HttpGet]
        [Route("GetAllSubDepartments")]
        public async Task<ActionResult> GetAllSubDepartments([FromQuery] int departmentId)
        {
            try
            {
                var result = await _subdeptRepository.GetSubDepartmentAsync(departmentId);

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve sub department list",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Sub department list retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sub department list");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new List<object>()
                });
            }
        }

        [HttpGet]
        [Route("GetEmployeeBySubDepartment")]
        public async Task<ActionResult> GetEmployeeBySubDepartment([FromQuery] int subDepartmentId)
        {
            try
            {
                var result = await _subdeptRepository.GetBranchWiseUsersAsync(subDepartmentId);

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve branch wise user list",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Branch wise user list retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch wise user list");
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
