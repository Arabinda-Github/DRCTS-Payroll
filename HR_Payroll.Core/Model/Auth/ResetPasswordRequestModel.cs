using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Auth
{
    public class ResetPasswordRequestModel
    {
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
    }
}
