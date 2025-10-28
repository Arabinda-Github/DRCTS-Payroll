using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Email
{
    public class BulkEmailResult
    {
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public List<EmailResult>? Results { get; set; }
    }
}
