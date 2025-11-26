using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.DTO.Leave
{
    public class ApplyLeaveRequestDto
    {
        public int EmployeeID { get; set; } 
        public int LeaveTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public IFormFile? Attachment { get; set; } 
        public string? LeaveFile { get; set; }
    }
}
