using Dapper;
using HR_Payroll.Core.DTO;
using HR_Payroll.Core.Entity;
using HR_Payroll.Core.Model;
using HR_Payroll.Core.Services;
using HR_Payroll.Infrastructure.Data;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Infrastructure.Concrete
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(AppDbContext context, ILogger<AttendanceService> logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Result<AttendanceResponseModel>> CheckInAsync(CheckInRequestModel model)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmployeeID", model.EmployeeID);
                parameters.Add("@Latitude", model.Latitude);
                parameters.Add("@Longitude", model.Longitude);
                parameters.Add("@Location", model.Location);
                parameters.Add("@Address", model.Address);
                parameters.Add("@IPAddress", model.IPAddress);
                parameters.Add("@DeviceInfo", model.DeviceInfo);
                parameters.Add("@Remarks", model.Remarks);
                parameters.Add("@CreatedBy", model.ModifiedBy);
                parameters.Add("@IsSuccess", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
                parameters.Add("@CheckInTime", dbType: DbType.Time, size: 7, direction: ParameterDirection.Output);
                parameters.Add("@IsWithinGeofence", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@AttendanceID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Distance", dbType: DbType.Decimal, precision: 10, scale: 2, direction: ParameterDirection.Output);

                await connection.ExecuteAsync("[dbo].[sp_ProcessCheckIn]", parameters, commandType: CommandType.StoredProcedure);

                return new Result<AttendanceResponseModel>
                {
                    IsSuccess = parameters.Get<bool>("@IsSuccess"),
                    Message = parameters.Get<string>("@Message"),
                    Entity = new AttendanceResponseModel
                    {
                        CheckInTime = parameters.Get<TimeSpan?>("@CheckInTime")
                    }
                };
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in User Check-in - Number: {ErrorNumber}, Message: {Message}",
                    sqlEx.Number, sqlEx.Message);

                if (sqlEx.Number == 18456) // Login failed
                {
                    _logger.LogError("Check-in failed error - Check SQL Server Check-in process  and 'sa' account status");
                    return Result<AttendanceResponseModel>.Failure("Database Check-in failed. Please check server configuration.");
                }

                return Result<AttendanceResponseModel>.Failure($"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                Log.Information($"Error in getting Check-in process: {ex.Message}");
                _logger.LogError(ex, "Error in Check-in process");
                return Result<AttendanceResponseModel>.Failure("Error in getting Check-in process");
            }

        }

        public async Task<Result<AttendanceResponseModel>> CheckOutAsync(CheckOutRequestModel model)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmployeeID", model.EmployeeID);
                parameters.Add("@Latitude", model.Latitude);
                parameters.Add("@Longitude", model.Longitude);
                parameters.Add("@Location", model.Location);
                parameters.Add("@Address", model.Address);
                parameters.Add("@IPAddress", model.IPAddress);
                parameters.Add("@DeviceInfo", model.DeviceInfo);
                parameters.Add("@Remarks", model.Remarks);
                parameters.Add("@ModifiedBy", model.ModifiedBy);
                parameters.Add("@IsSuccess", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
                parameters.Add("@CheckOutTime", dbType: DbType.Time, size: 7, direction: ParameterDirection.Output);
                parameters.Add("@WorkingHours", dbType: DbType.Decimal, precision: 10, scale: 2, direction: ParameterDirection.Output);
                parameters.Add("@IsWithinGeofence", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@Distance", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                await connection.ExecuteAsync("[dbo].[sp_ProcessCheckOut]", parameters, commandType: CommandType.StoredProcedure);

                return new Result<AttendanceResponseModel>
                {
                    IsSuccess = parameters.Get<bool>("@IsSuccess"),
                    Message = parameters.Get<string>("@Message"),
                    Entity = new AttendanceResponseModel
                    {
                        CheckOutTime = parameters.Get<TimeSpan?>("@CheckOutTime"),
                        WorkingHours = parameters.Get<decimal?>("@WorkingHours")
                    }
                };
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in User Check-out - Number: {ErrorNumber}, Message: {Message}",
                    sqlEx.Number, sqlEx.Message);

                if (sqlEx.Number == 18456) // Login failed
                {
                    _logger.LogError("Check-out failed error - Check SQL Server Check-out process  and 'sa' account status");
                    return Result<AttendanceResponseModel>.Failure("Database Check-out failed. Please check server configuration.");
                }

                return Result<AttendanceResponseModel>.Failure($"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                Log.Information($"Error in getting Check-out process: {ex.Message}");
                _logger.LogError(ex, "Error in Check-out process");
                return Result<AttendanceResponseModel>.Failure("Error in getting Check-out process");
            }
        }

        public async Task<Result<AttendanceStatusResponseModel>> GetCurrentStatusAsync(int employeeId)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var result = await connection.QueryFirstOrDefaultAsync<AttendanceStatusResponseModel>(
                    "[dbo].[sp_GetCurrentAttendanceStatus]",
                    new { EmployeeID = employeeId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                if (result == null)
                {
                    return new Result<AttendanceStatusResponseModel>
                    {
                        IsSuccess = false,
                        Message = "Employee not found or inactive.",
                        Entity = null
                    };
                }

                return new Result<AttendanceStatusResponseModel>
                {
                    IsSuccess = true,
                    Message = "Attendance status retrieved successfully.",
                    Entity = result
                };

            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Database error while fetching attendance status for employee {EmployeeId}", employeeId);
                return new Result<AttendanceStatusResponseModel>
                {
                    IsSuccess = false,
                    Message = "Database error occurred. Please try again later.",
                    Entity = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching attendance status for employee {EmployeeId}", employeeId);
                return new Result<AttendanceStatusResponseModel>
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred. Please contact support.",
                    Entity = null
                };
            }
        }

        public async Task<PagedResult<AttendanceDto>> GetAttendanceReportAsync(AttendanceRequestModel model)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmployeeID", model.EmployeeId);
                parameters.Add("@FromDate", model.FromDate);
                parameters.Add("@ToDate", model.ToDate);
                parameters.Add("@PageNumber", model.Start);
                parameters.Add("@PageSize", model.Length);

                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                using (var multi = await connection.QueryMultipleAsync(
                    "[dbo].[sp_GetEmployeeAttendanceReport]",
                    parameters,
                    commandType: CommandType.StoredProcedure))
                {
                    var data = (await multi.ReadAsync<AttendanceDto>()).ToList();

                    int totalCount = data.Count;

                    if (data == null)
                    {
                        return new PagedResult<AttendanceDto>
                        {
                            IsSuccess = false,
                            Message = "Attendance not found.",
                            Entity = null
                        };
                    }

                    return new PagedResult<AttendanceDto>
                    {
                        IsSuccess = true,
                        Message = "Attendance retrieved successfully.",
                        Entity = data,
                        TotalCount = totalCount,
                        PageNumber = model.Start,
                        PageSize = model.Length
                    };
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Database error while fetching attendance for employee {EmployeeId}", model.EmployeeId);
                return new PagedResult<AttendanceDto>
                {
                    IsSuccess = false,
                    Message = "Database error occurred. Please try again later.",
                    Entity = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching attendance for employee {EmployeeId}", model.EmployeeId);
                return new PagedResult<AttendanceDto>
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred. Please contact support.",
                    Entity = null
                };
            }
        }

        public async Task<Result<List<CalenderResponseModel>>> GetAttendanceCalendarAsync(CalenderRequestModel model)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();
                var result = await connection.QueryAsync<CalenderResponseModel>(
                    "[dbo].[sp_GetEmployeeAttendanceCalendar]",
                    new { EmployeeID = model.EmployeeId, FromDate = model.FromDate, ToDate = model.ToDate },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );
                return new Result<List<CalenderResponseModel>>
                {
                    IsSuccess = true,
                    Message = "Attendance calendar retrieved successfully.",
                    Entity = result.ToList()
                };
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Database error while fetching attendance calendar for employee {EmployeeId}", model.EmployeeId);
                return new Result<List<CalenderResponseModel>>
                {
                    IsSuccess = false,
                    Message = "Database error occurred. Please try again later.",
                    Entity = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching attendance calendar for employee {EmployeeId}", model.EmployeeId);
                return new Result<List<CalenderResponseModel>>
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred. Please contact support.",
                    Entity = null
                };
            }
        }
    }
}
