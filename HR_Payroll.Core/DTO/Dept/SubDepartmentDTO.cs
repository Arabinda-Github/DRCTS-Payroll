using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.DTO.Dept
{
    public class SubDepartmentDTO
    {
        public int SubDepartmentId { get; set; }
        public int DepartmentId { get; set; }
        public string? SubDepartmentName { get;set; }
    }
}
