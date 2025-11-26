using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Leave
{
    public class EmployeeLeaveRequest
    {
        public int LeaveId { get; set; }
        public int EmployeeID { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string? Del_Flg { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? Remarks { get; set; }
        public string? Attachment { get; set; }
    }
}
