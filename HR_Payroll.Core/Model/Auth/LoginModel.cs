using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Auth
{
    public class LoginModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool RememberMe { get; set; } = false;
        public string? LoginIP { get; set; }
        public string LoginType { get; set; } = "System";
    }
}
