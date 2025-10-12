using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Response
{
    public class LoginResponse<T>
    {
        public bool status { get; set; }
        public string? message { get; set; }
        public T? data { get; set; } = default(T);       
    }
    // Define a class to hold token info
    public class TokenData
    {
        public string? accessToken { get; set; }
        public string? refreshToken { get; set; }
        public DateTime expiresAt { get; set; }
    }
}
