using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.DataTable
{
    public class PaginationDataRequestModel
    {
        public int Start { get; set; } = 0;
        public int Length { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; } = "ASC";
    }
}
