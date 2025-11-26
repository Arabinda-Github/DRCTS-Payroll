using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Leave
{
    public class LeaveType
    {
        public int LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }
        public decimal AnnualQuota { get; set; }
        public bool CarryForward { get; set; }
        public bool IsActive { get; set; }
    }
}
