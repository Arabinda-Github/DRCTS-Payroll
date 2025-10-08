using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.DTO
{
    public class ApplicationUserDto
    {
        public int RegId { get; set; }

        public string? EmailId { get; set; }
                     
        public string? FirstName { get; set; }
                     
        public string? LastName { get; set; }

        public bool IsActive { get; set; }

        public string? UserName { get; set; }

        public List<ApplicationUserRoleDto>? UserRoles { get; set; }

        public string? Password { get; set; }
    }
}
