using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Master
{
    public class BranchWiseUserModel
    {
        public int EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public int UserID { get; set; }
        public string? Role { get; set; }
        public string? DepartmentName { get; set; }
        public string? SubDepartmentName { get; set; }
    }

}
