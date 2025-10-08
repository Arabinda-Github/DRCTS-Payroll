using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Response
{
    public class LoginResponse
    {
        public bool status { get; set; }
        public string message { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;
    }
}
