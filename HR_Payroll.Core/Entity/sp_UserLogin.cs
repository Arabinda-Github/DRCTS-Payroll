using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Entity
{
    public class sp_UserLogin
    {
        [Key]
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? MobileNumber { get; set; }
        public int UserTypeId { get; set; }
        public string? UserTypeName { get; set; }
        public int EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? ProfilePic { get; set; }
        public bool IsTwoFactorEnabled { get; set; } = false;
        public bool AccountLocked { get; set; } = false;

        // Message & status from procedure
        public string? Message { get; set; }
        public bool Success { get; set; } = false;
    }
}
