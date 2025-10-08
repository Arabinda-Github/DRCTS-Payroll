using HR_Payroll.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Services
{
    public class Result
    {
        private Result(ResultStatusType status, string message = null)
        {
            this.status = status;
            this.message = message;
        }

        public string message { get; set; }

        public ResultStatusType status { get; }

        public static Result Failure(string message = null)
        {
            return new Result(ResultStatusType.Failure, message);
        }

        public static Result NotFound()
        {
            return new Result(ResultStatusType.NotFound);
        }

        public static Result Success()
        {
            return new Result(ResultStatusType.Success);
        }
    }
}
