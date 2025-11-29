using HR_Payroll.Core.DTO.Leave;
using HR_Payroll.Core.Model.Leave;
using HR_Payroll.Core.Services;
using System.Threading.Tasks;

namespace HR_Payroll.Infrastructure.Interface
{
    public interface ILeaveService
    {
        Task<Result<List<LeaveType>>> GetAllLeaveTypes();
        Task<Result<EmployeeLeaveBalanceResponse>> GetEmployeeLeaveBalance(int employeeId);
        Task<Result<List<EmployeeLeaveRequest>>> GetAllLeaveRequests(int employeeId);
        Task<Result<EmployeeLeaveRequest>> GetLeaveRequestById(int leaveId);
        Task<Result<bool>> ApplyLeaveAsync(ApplyLeaveRequestDto request);
        Task<Result<bool>> ProcessEmployeeLeave(ProcessLeaveRequest model);
        Task<Result<bool>> HasPendingLeave(int empId);
        Task<Result<List<PendingLeaveDto>>> GetPendingLeaveRequests(int supervisorId);
    }
}
