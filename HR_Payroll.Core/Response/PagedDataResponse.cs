using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Response
{
    public class PagedDataResponse<T>
    {
        public bool status { get; set; }
        public string message { get; set; } = string.Empty;
        public ReturnData<T>? data { get; set; }
    }

    public class ReturnData<T>
    {
        public int totalCount { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public List<T>? records { get; set; }
    }
}
