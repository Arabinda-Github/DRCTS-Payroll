using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Leave
{
    public class ProcessLeaveRequest
    {
        public int LeaveId { get; set; }
        public int ApprovedBy { get; set; }
        public string? Action { get; set; }   // "Approve" or "Reject"
        public string? Remark { get; set; }
    }
}
