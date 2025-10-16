using Dapper;
using HR_Payroll.Core.Entity;
using HR_Payroll.Core.Model;
using HR_Payroll.Core.Services;
using HR_Payroll.Infrastructure.Data;
using HR_Payroll.Infrastructure.Interface;
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

                await connection.ExecuteAsync("[drconnect123].[sp_ProcessCheckIn]", parameters, commandType: CommandType.StoredProcedure);

                return new Result<AttendanceResponseModel>
                {
                    IsSuccess = parameters.Get<bool>("@IsSuccess"),
                    Message = parameters.Get<string>("@Message"),
                    Entity = new AttendanceResponseModel
                    {
                        CheckInTime = parameters.Get<TimeSpan>("@CheckInTime")
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
                await connection.ExecuteAsync("[drconnect123].[sp_ProcessCheckOut]", parameters, commandType: CommandType.StoredProcedure);

                return new Result<AttendanceResponseModel>
                {
                    IsSuccess = parameters.Get<bool>("@IsSuccess"),
                    Message = parameters.Get<string>("@Message"),
                    Entity = new AttendanceResponseModel
                    {
                        CheckOutTime = parameters.Get<TimeSpan>("@CheckOutTime"),
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
    }
}
