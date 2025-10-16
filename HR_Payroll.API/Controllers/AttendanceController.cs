using HR_Payroll.Core.Model;
using HR_Payroll.Core.Response;
using HR_Payroll.Infrastructure.Concrete;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Http;
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

        [HttpPost]
        [Route("CheckIn")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequestModel model)
        {
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
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequestModel model)
        {
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

    }
}
