using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Response
{
    public class DataResponse<T>
    {
        public bool status { get; set; }
        public string message { get; set; }
        public T? data { get; set; } = default(T);
    }
}
