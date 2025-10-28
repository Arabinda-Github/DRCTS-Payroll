using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Auth
{
    public class PasswordResetToken
    {
        public Guid TokenId { get; set; }
        public int UserId { get; set; }
        public string? ResetToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
