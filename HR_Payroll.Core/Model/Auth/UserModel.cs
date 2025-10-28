using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Auth
{
    public class UserModel
    {
        public int UserID { get; set; }
        public int UserTypeId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? MobileNumber { get; set; }
        public string? PasswordHash { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool IsMobileVerified { get; set; } = false;
        public bool IsTwoFactorEnabled { get; set; } = false;
        public string Status { get; set; } = "Active";
        public char? Del_Flg { get; set; }
        public DateTime? CreatedDate { get; set; } 
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; } 
        public string? ModifiedBy { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool? LoggedIn { get; set; }
        public bool? AccountLocked { get; set; }
        public int? LoginFailureAttempt { get; set; }
        public int? AccountStatusID { get; set; }
        public DateTime? AccountLockedDate { get; set; }
    }
}
