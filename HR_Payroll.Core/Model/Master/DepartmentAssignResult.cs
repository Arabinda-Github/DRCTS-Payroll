using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Master
{
    public class DepartmentAssignResult
    {
        public bool Status { get; set; }
        public string? Message { get; set; }
        public int? DepartmentAssigID { get; set; }
    }
}
