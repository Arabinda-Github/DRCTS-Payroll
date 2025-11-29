using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.DTO.Leave
{
    public class PendingLeaveDto
    {
        public int LeaveId { get; set; }
        public int EmployeeID { get; set; }
        public string? EmployeeName { get; set; }
        public string? UserType { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public string? Attachment { get; set; }
        public DateTime CreatedOn { get; set; }

    }
}
