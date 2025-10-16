using HR_Payroll.Core.Model;
using HR_Payroll.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Infrastructure.Interface
{
    public interface IAttendanceService
    {
        Task<Result<AttendanceResponseModel>> CheckInAsync(CheckInRequestModel request);
        Task<Result<AttendanceResponseModel>> CheckOutAsync(CheckOutRequestModel request);
        //Task<AttendanceStatusResponseModel> GetCurrentStatusAsync(int employeeId);
        //Task<ShiftDetails> GetEmployeeShiftDetailsAsync(int employeeId, DateTime? checkDate = null);
    }
}
