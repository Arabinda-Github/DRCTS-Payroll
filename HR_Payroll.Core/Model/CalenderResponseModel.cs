using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model
{
    public class CalenderResponseModel
    {
        public DateTime AttendanceDate { get; set; }

        public string? Status { get; set; }

        public string? Remarks { get; set; }
    }
}
