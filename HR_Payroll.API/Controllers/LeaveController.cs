using Azure.Core;
using HR_Payroll.CommonCases.Utility;
using HR_Payroll.Core.DTO.Leave;
using HR_Payroll.Core.Model.Leave;
using HR_Payroll.Core.Response;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveController : ControllerBase
    {
        private readonly ILogger<LeaveController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILeaveService _leaveservice;

        public LeaveController(IConfiguration configuration, ILeaveService leaveservice,
           ILogger<LeaveController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _leaveservice = leaveservice;

        }

        public string fileDirectory => _configuration.GetValue<string>("FileUploadSettings:BaseDirectory");
        private int GetEmployeeIdFromClaims()
        {
            var empIdClaim = HttpContext?.User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(empIdClaim, out int empId) ? empId : 0;
        }

        [HttpGet]
        [Route("GetAllLeaveTypes")]
        [Authorize(Roles = "Admin,Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> GetAllLeaveTypes()
        {
            try
            {
                var result = await _leaveservice.GetAllLeaveTypes();

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve leave types",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Leave types retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave types");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new List<object>()
                });
            }
        }

        [HttpGet]
        [Route("GetEmployeeLeaveBalance")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> GetEmployeeLeaveBalance()
        {
            try
            {
                var employeeId = GetEmployeeIdFromClaims();
                var result = await _leaveservice.GetEmployeeLeaveBalance(employeeId);

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve employee leave balance",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Employee leave retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee leave");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new List<object>()
                });
            }

        }

        [HttpGet]
        [Route("GetAllLeaveRequests")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> GetAllLeaveRequests()
        {
            try
            {
                var employeeId = GetEmployeeIdFromClaims();
                var result = await _leaveservice.GetAllLeaveRequests(employeeId);

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve employee leave requests",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Employee leave requests retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee leave requests");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new List<object>()
                });
            }
        }

        [HttpGet]
        [Route("GetLeaveRequestById")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> GetLeaveRequestById([FromQuery] int leaveId)
        {
            try
            {
                var result = await _leaveservice.GetLeaveRequestById(leaveId);

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve employee leave request",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Employee leave request retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee leave request");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new List<object>()
                });
            }
        }

        [HttpPost]
        [Route("ApplyLeave")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> ApplyLeave([FromForm] ApplyLeaveRequestDto model)
        {
            model.EmployeeID = GetEmployeeIdFromClaims();
            if (model == null)
            {
                return Ok(new DataResponse<object>
                {
                    status = false,
                    message = "Invalid request",
                    data = new List<object>()
                });
            }
            if (model.EmployeeID <= 0)
            {
                return Ok(new DataResponse<object>
                {
                    status = false,
                    message = "Invalid Employee ID",
                    data = new List<object>()
                });
            }

            try
            {
                if (model.Attachment != null)
                {
                    FileInfo fi = new FileInfo(model.Attachment.FileName);
                    string file = ExternalHelper.FileUploadThroughApi(model.Attachment, fileDirectory, "Leave_Attachment");
                    model.LeaveFile = file;
                }
                var result = await _leaveservice.ApplyLeaveAsync(model);

                if (result.IsSuccess && result.Entity)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = result.IsSuccess,
                        message = result.Message ?? "Leave applied successfully",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = result.IsSuccess,
                    message = result.Message ?? "Leave application failed",
                    data = new List<object>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during leave application: {ex.Message}");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing leave application",
                    data = new List<object>()
                });
            }
        }

        [HttpPost]
        [Route("LeaveProcess")]
        [Authorize(Roles = "HR,Manager,Team Lead")]
        public async Task<IActionResult> ProcessLeave([FromBody] ProcessLeaveRequest model)
        {
            model.ApprovedBy = GetEmployeeIdFromClaims();
            if (model == null)
            {
                return Ok(new DataResponse<object>
                {
                    status = false,
                    message = "Invalid request",
                    data = new List<object>()
                });
            }
            if (model.ApprovedBy <= 0)
            {
                return Ok(new DataResponse<object>
                {
                    status = false,
                    message = "Invalid Approved By",
                    data = new List<object>()
                });
            }

            try
            {
                var result = await _leaveservice.ProcessEmployeeLeave(model);

                if (result.IsSuccess && result.Entity)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = result.IsSuccess,
                        message = result.Message ?? "Leave processed successfully",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = result.IsSuccess,
                    message = result.Message ?? "Leave processing failed",
                    data = new List<object>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during leave processing: {ex.Message}");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing leave processing",
                    data = new List<object>()
                });
            }
        }

        [HttpGet]
        [Route("CheckPendingLeave")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> CheckPendingLeave()
        {
            try
            {
                var employeeId = GetEmployeeIdFromClaims();
                var result = await _leaveservice.HasPendingLeave(employeeId);

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to check pending leave",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Check pending leave successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending leave");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new List<object>()
                });
            }

        }

        [HttpGet]
        [Route("PendingLeaveRequests")]
        [Authorize(Roles = "Employee,Manager,Team Lead")]
        public async Task<IActionResult> PendingLeaveRequests()
        {
            try
            {
                var employeeId = GetEmployeeIdFromClaims();
                var result = await _leaveservice.GetPendingLeaveRequests(employeeId);

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve leave requests",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Leave requests retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave requests");
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
