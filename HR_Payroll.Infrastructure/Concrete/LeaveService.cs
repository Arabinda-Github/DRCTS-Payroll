using Dapper;
using HR_Payroll.Core.DTO.Leave;
using HR_Payroll.Core.Model.Leave;
using HR_Payroll.Core.Services;
using HR_Payroll.Infrastructure.Data;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace HR_Payroll.Infrastructure.Concrete
{
    public class LeaveService : ILeaveService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LeaveService> _logger;

        public LeaveService(AppDbContext context, ILogger<LeaveService> logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Result<EmployeeLeaveBalanceResponse>> GetEmployeeLeaveBalance(int employeeId)
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();

                var sql = @"
                    SELECT 
                        elb.EmployeeId,
                        elb.LeaveTypeId,
                        lm.LeaveTypeName,
                        elb.OpeningBalance,
                        elb.LeavesTaken,
                        elb.ClosingBalance
                    FROM EmployeeLeaveBalance elb
                    INNER JOIN LeaveMaster lm 
                        ON elb.LeaveTypeId = lm.LeaveTypeId
                    WHERE elb.EmployeeId = @EmployeeId;
                ";

                var detailList = (await connection.QueryAsync<EmployeeLeaveBalanceDto>(
                    sql, new { EmployeeId = employeeId }
                )).ToList();

                if (!detailList.Any())
                {
                    return new Result<EmployeeLeaveBalanceResponse>
                    {
                        IsSuccess = false,
                        Message = "Employee not found or no leave balance exists.",
                        Entity = null
                    };
                }

                var response = new EmployeeLeaveBalanceResponse
                {
                    EmployeeId = employeeId,
                    TotalOpeningBalance = detailList.Sum(x => x.OpeningBalance),
                    TotalLeavesTaken = detailList.Sum(x => x.LeavesTaken),
                    TotalClosingBalance = detailList.Sum(x => x.ClosingBalance),
                    LeaveDetails = detailList
                };

                return new Result<EmployeeLeaveBalanceResponse>
                {
                    IsSuccess = true,
                    Message = "Leave balance retrieved successfully.",
                    Entity = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave balance for EmployeeId: {EmployeeId}", employeeId);

                return new Result<EmployeeLeaveBalanceResponse>
                {
                    IsSuccess = false,
                    Message = "An internal error occurred while retrieving leave balance.",
                    Entity = null
                };
            }
        }

        public async Task<Result<List<LeaveType>>> GetAllLeaveTypes()
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();

                var sql = @"
                SELECT 
                    LeaveTypeId, 
                    LeaveTypeName, 
                    AnnualQuota, 
                    CarryForward,
                    IsActive
                    FROM LeaveMaster;
                ";

                var leaveTypes = (await connection.QueryAsync<LeaveType>(sql)).ToList();

                return new Result<List<LeaveType>>
                {
                    IsSuccess = true,
                    Message = "Leave types retrieved successfully.",
                    Entity = leaveTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching leave types");

                return new Result<List<LeaveType>>
                {
                    IsSuccess = false,
                    Message = $"Failed to fetch leave types. Details: {ex.Message}",
                    Entity = null
                };
            }
        }

        public async Task<Result<List<EmployeeLeaveRequest>>> GetAllLeaveRequests(int employeeId)
        {
            try
            {
                using var conn = _context.Database.GetDbConnection();

                var sql = @"
                    SELECT 
                        L.LeaveId,
                        L.EmployeeID,
                        L.LeaveTypeId,
                        LT.LeaveTypeName AS LeaveType,  
                        L.FromDate,
                        L.ToDate,
                        L.TotalDays,
                        L.Reason,
                        L.Status,
                        L.ApprovedBy,
                        L.ApprovedOn,
                        L.Del_Flg,
                        L.CreatedOn,
                        L.ModifiedOn,
                        L.Remarks,
                        L.Attachment
                    FROM EmployeeLeaveRequests L
                    LEFT JOIN LeaveMaster LT ON LT.LeaveTypeId = L.LeaveTypeId
                    WHERE L.Del_Flg = 'N'
                      AND (L.EmployeeID = @EmployeeId OR @EmployeeId = 0);
                ";

                var result = (await conn.QueryAsync<EmployeeLeaveRequest>(sql, new { EmployeeId = employeeId })).ToList();

                return new Result<List<EmployeeLeaveRequest>>
                {
                    IsSuccess = true,
                    Message = "Leave requests fetched successfully.",
                    Entity = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching leave requests");
                return new Result<List<EmployeeLeaveRequest>>
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Entity = null
                };
            }
        }

        public async Task<Result<EmployeeLeaveRequest>> GetLeaveRequestById(int leaveId)
        {
            try
            {
                using var conn = _context.Database.GetDbConnection();

                var sql = @"
                SELECT 
                    LeaveId, EmployeeID, LeaveTypeId, FromDate, ToDate,
                    TotalDays, Reason, Status, ApprovedBy, ApprovedOn,
                    Del_Flg, CreatedOn, ModifiedOn, Remarks, Attachment
                FROM EmployeeLeaveRequests
                WHERE LeaveId = @LeaveId AND Del_Flg = 'N';
            ";

                var record = await conn.QueryFirstOrDefaultAsync<EmployeeLeaveRequest>(sql, new { LeaveId = leaveId });

                if (record == null)
                {
                    return new Result<EmployeeLeaveRequest>
                    {
                        IsSuccess = false,
                        Message = "Leave request not found.",
                        Entity = null
                    };
                }

                return new Result<EmployeeLeaveRequest>
                {
                    IsSuccess = true,
                    Message = "Leave request fetched successfully.",
                    Entity = record
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching leave request by ID");
                return new Result<EmployeeLeaveRequest>
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Entity = null
                };
            }
        }

        public async Task<Result<bool>> ApplyLeaveAsync(ApplyLeaveRequestDto request)
        {
            try
            {
                using var conn = _context.Database.GetDbConnection();

                var sql = @"
                INSERT INTO EmployeeLeaveRequests
                (
                    EmployeeID, LeaveTypeId, FromDate, ToDate, TotalDays,
                    Reason, Status, Del_Flg, CreatedOn, Attachment
                )
                VALUES
                (
                    @EmployeeID, @LeaveTypeId, @FromDate, @ToDate, @TotalDays,
                    @Reason, 'Pending', 'N', GETDATE(), @LeaveFile
                );
            ";

                var rows = await conn.ExecuteAsync(sql, request);

                return new Result<bool>
                {
                    IsSuccess = rows > 0,
                    Message = rows > 0 ? "Leave applied successfully" : "Failed to apply leave",
                    Entity = rows > 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying leave");
                return new Result<bool>
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Entity = false
                };
            }
        }

        public async Task<Result<bool>> ProcessEmployeeLeave(ProcessLeaveRequest model)
        {
            try
            {
                using var conn = _context.Database.GetDbConnection();

                var parameters = new DynamicParameters();
                parameters.Add("@LeaveId", model.LeaveId);
                parameters.Add("@ApprovedBy", model.ApprovedBy);
                parameters.Add("@Action", model.Action);
                parameters.Add("@Remark", model.Remark);

                await conn.ExecuteAsync(
                    "sp_ProcessEmployeeLeave",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return new Result<bool>
                {
                    IsSuccess = true,
                    Message = "Leave processed successfully.",
                    Entity = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_ProcessEmployeeLeave");

                return new Result<bool>
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Entity = false
                };
            }
        }

        public async Task<Result<bool>> HasPendingLeave(int empId)
        {
            try
            {
                using var conn = _context.Database.GetDbConnection();

                var sql = @"
                    SELECT COUNT(*)
                    FROM EmployeeLeaveRequests
                    WHERE EmployeeID = @empId 
                      AND Status = 'Pending' 
                      AND Del_Flg = 'N'
                ";

                // Execute query
                var pendingCount = await conn.ExecuteScalarAsync<int>(sql, new { empId });

                return new Result<bool>
                {
                    IsSuccess = true,
                    Message = pendingCount > 0
                        ? "Pending leave found."
                        : "No pending leave.",
                    Entity = pendingCount > 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing HasPendingLeave");

                return new Result<bool>
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Entity = false
                };
            }
        }

        public async Task<Result<List<PendingLeaveDto>>> GetPendingLeaveRequests(int supervisorId)
        {
            try
            {
                using var conn = _context.Database.GetDbConnection();

                var result = (await conn.QueryAsync<PendingLeaveDto>(
                    "dbo.sp_GetPendingLeaveRequests",
                    new { SupervisorId = supervisorId },
                    commandType: CommandType.StoredProcedure
                )).ToList();

                return new Result<List<PendingLeaveDto>>
                {
                    IsSuccess = true,
                    Message = "Pending leave requests fetched successfully.",
                    Entity = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending leave requests via stored procedure");
                return new Result<List<PendingLeaveDto>>
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Entity = null
                };
            }
        }


    }
}
