using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.DTO.Leave
{
    public class EmployeeLeaveBalanceDto
    {
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal LeavesTaken { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}
