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
            Status = status;
            Message = message;
        }

        public string Message { get; }
        public ResultStatusType Status { get; }

        // Failure factory
        public static Result Failure(string message = null)
        {
            return new Result(ResultStatusType.Failure, message);
        }

        // NotFound factory
        public static Result NotFound(string message = null)
        {
            return new Result(ResultStatusType.NotFound, message);
        }

        // Success factory
        public static Result Success(string message = null)
        {
            return new Result(ResultStatusType.Success, message);
        }

        // Convenience property
        public bool IsSuccess => Status == ResultStatusType.Success;
    }

}
