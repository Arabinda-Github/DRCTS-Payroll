using HR_Payroll.Core.DTO.Dept;
using HR_Payroll.Core.Model.DataTable;
using HR_Payroll.Core.Model.Master;
using HR_Payroll.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Infrastructure.Interface
{
    public interface IDepartmentServices
    {
        Task<Result<IEnumerable<DepartmentDTO>>> GetDepartmentsAsync();
        Task<Result<DepartmentAssignResult>> AssignDepartmentHierarchyAsync(DepartmentAssignDTO dto);
        Task<Result<IEnumerable<AssignEmployeeListModel>>> GetAssignListAsync(PaginationDataRequestModel model);
    }
}
