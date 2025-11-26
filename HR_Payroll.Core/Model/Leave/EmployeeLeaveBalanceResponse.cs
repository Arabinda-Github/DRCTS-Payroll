using HR_Payroll.Core.DTO.Leave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Leave
{
    public class EmployeeLeaveBalanceResponse
    {
        public int EmployeeId { get; set; }
        public decimal TotalOpeningBalance { get; set; }
        public decimal TotalLeavesTaken { get; set; }
        public decimal TotalClosingBalance { get; set; }
        public List<EmployeeLeaveBalanceDto> LeaveDetails { get; set; } = new List<EmployeeLeaveBalanceDto>();
    }
}
