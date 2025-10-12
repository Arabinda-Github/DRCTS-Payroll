using HR_Payroll.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Services
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; }=false;
        public string? Message { get; set; }
        public T? Entity { get; set; }

        // Success factory method
        public static Result<T> Success(T entity, string message = null)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Entity = entity,
                Message = message
            };
        }

        // Failure factory method
        public static Result<T> Failure(string message, T entity = default)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Entity = entity,
                Message = message
            };
        }
    }
}
