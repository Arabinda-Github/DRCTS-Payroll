using HR_Payroll.Core.DTO;
using HR_Payroll.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Services
{
    public static class ApplicationUserRoleClaims
    {
        private static readonly Dictionary<ApplicationUserRole, string> FriendlyNames = new Dictionary<ApplicationUserRole, string>
            {
                // NOTE: These names must match exactly with the names in the database.
                { ApplicationUserRole.None, "None" },
                { ApplicationUserRole.Admin, "Admin" },
                { ApplicationUserRole.HR, "HR" },
                { ApplicationUserRole.Employee, "Employee" },
                { ApplicationUserRole.Manager, "Manager" },
                { ApplicationUserRole.TeamLead, "Team Lead" }
            };

        public static IEnumerable<ApplicationUserRoleDto> GetApplicationUserRoles()
        {
            return FriendlyNames
                .Where(x => x.Key != ApplicationUserRole.None)
                .OrderBy(x => x.Value)
                .Select(x => new ApplicationUserRoleDto { Id = x.Key, Name = x.Value });
        }

        public static string ToFriendlyName(this ApplicationUserRole role)
        {
            if (!FriendlyNames.TryGetValue(role, out var friendlyName))
            {
                throw new ArgumentOutOfRangeException(nameof(role), $"{nameof(role)} has no friendly name.");
            }

            return friendlyName;
        }
    }
}
