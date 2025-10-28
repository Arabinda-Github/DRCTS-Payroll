using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Email
{
    public class EmailResult
    {
        public bool success { get; set; }
        public string? message { get; set; }

        public static EmailResult Success(string message = "Email sent successfully")
        {
            return new EmailResult { success = true, message = message };
        }

        public static EmailResult Failure(string message)
        {
            return new EmailResult { success = false, message = message };
        }
    }
}
