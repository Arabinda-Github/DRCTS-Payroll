using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.DTO.Dept
{
    public class DepartmentAssignDTO
    {
        public int DepartmentId { get; set; }
        public int SubDepartmentId { get; set; }
        public int ManagerId { get; set; }
        public int TeamLeadId { get; set; }
        public string? EmployeeId { get; set; }
        public string? CreatedBy { get; set; }
        public string? Remarks { get; set; }
    }
}
