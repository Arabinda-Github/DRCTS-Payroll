using HR_Payroll.Core.Model;
using HR_Payroll.Core.Response;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly ILogger<AttendanceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAttendanceService _attendanceRepository;

        public AttendanceController(IConfiguration configuration,
            ILogger<AttendanceController> logger,
            IAttendanceService attendanceRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _attendanceRepository = attendanceRepository;

        }

        private int GetEmployeeIdFromClaims()
        {
            var empIdClaim = HttpContext?.User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(empIdClaim, out int empId) ? empId : 0;
        }

        [HttpPost]
        [Route("CheckIn")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequestModel model)
        {
            model.EmployeeID = GetEmployeeIdFromClaims();
            if (model == null || model.EmployeeID <= 0)
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
                var result = await _attendanceRepository.CheckInAsync(model);

                if (result.IsSuccess && result.Entity != null)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = result.IsSuccess,
                        message = result.Message ?? "Check-in successful",
                        data = new List<object> { new { CheckInDetails = result.Entity } }
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = result.IsSuccess,
                    message = result.Message ?? "Check-in failed",
                    data = new List<object>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during check-in: {ex.Message}");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing check-in",
                    data = new List<object>()
                });
            }
        }

        [HttpPost]
        [Route("CheckOut")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequestModel model)
        {
            model.EmployeeID = GetEmployeeIdFromClaims();
            if (model == null || model.EmployeeID <= 0)
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
                var result = await _attendanceRepository.CheckOutAsync(model);

                if (result.IsSuccess && result.Entity != null)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = result.IsSuccess,
                        message = result.Message ?? "Check-out successful",
                        data = new List<object> { new { CheckOutDetails = result.Entity } }
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = result.IsSuccess,
                    message = result.Message ?? "Check-out failed",
                    data = new List<object>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during check-out: {ex.Message}");
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing check-out",
                    data = new List<object>()
                });
            }
        }

        [HttpGet]
        [Route("GetAttendanceStatus")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<ActionResult> GetAttendanceStatus()
        {
            try
            {
                var employeeId = GetEmployeeIdFromClaims();
                if (employeeId <= 0)
                {
                    return BadRequest(new DataResponse<object>
                    {
                        status = false,
                        message = "Invalid employee ID",
                        data = new List<object>()
                    });
                }

                var result = await _attendanceRepository.GetCurrentStatusAsync(employeeId);

                if (!result.IsSuccess)
                {
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve attendance status",
                        data = new List<object>()
                    });
                }

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Status retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance status for employee {EmployeeId}",
                    GetEmployeeIdFromClaims());
                return StatusCode(500, new DataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new List<object>()
                });
            }
        }

        [HttpGet]
        [Route("GetAttendanceReport")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> GetAttendanceReport([FromQuery] AttendanceRequestModel model)
        {
            model.EmployeeId = GetEmployeeIdFromClaims();
            if (model.EmployeeId <= 0)
            {
                return BadRequest(new PagedDataResponse<object>
                {
                    status = false,
                    message = "Invalid employee ID"
                });
            }

            try
            {
                var result = await _attendanceRepository.GetAttendanceReportAsync(model);

                if (!result.IsSuccess || result.Entity == null)
                {
                    return Ok(new PagedDataResponse<object>
                    {
                        status = false,
                        message = result.Message ?? "Failed to retrieve attendance",
                        data = new ReturnData<object>()
                    });
                }
                var pagedResponse = new ReturnData<object>
                {
                    totalCount = result.TotalCount,   // ✅ ensure repo returns total count
                    pageNumber = model.Start,
                    pageSize = model.Length,
                    records = result.Entity.Cast<object>().ToList()  // safely convert to List<object>
                };
                return Ok(new PagedDataResponse<object>
                {
                    status = true,
                    message = result.Message ?? "Attendance retrieved successfully",
                    data = pagedResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance for employee {EmployeeId}", model.EmployeeId);

                return StatusCode(500, new PagedDataResponse<object>
                {
                    status = false,
                    message = "An error occurred while processing your request.",
                    data = new ReturnData<object>()
                });
            }
        }

        [HttpGet]
        [Route("GetAttendanceCalendar")]
        [Authorize(Roles = "Employee,HR,Manager,Team Lead")]
        public async Task<IActionResult> GetAttendanceCalendar([FromQuery] CalenderRequestModel model)
        {
            try
            {
                model.EmployeeId = GetEmployeeIdFromClaims();
                if (model.EmployeeId <= 0)
                {
                    return BadRequest(new DataResponse<object>
                    {
                        status = false,
                        message = "Invalid employee ID",
                        data = new List<object>()
                    });
                }
                var result = await _attendanceRepository.GetAttendanceCalendarAsync(model);

                if (!result.IsSuccess || result.Entity == null)
                    return Ok(new DataResponse<object>
                    {
                        status = false,
                        message = "Failed to retrieve calendar data",
                        data = new List<object>()
                    });

                return Ok(new DataResponse<object>
                {
                    status = true,
                    message = "Data retrieved successfully",
                    data = result.Entity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance status for employee {EmployeeId}",
                    GetEmployeeIdFromClaims());
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
