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
        private Result(ResultStatusType status, string message = null)
        {
            this.status = status;
            this.message = message;
        }

        private Result(ResultStatusType status, T entity, string message = null)
        {
            this.status = status;
            this.entity = entity;
            this.message = message;
        }

        public T entity { get; }

        public bool IsSuccess => this.status == ResultStatusType.Success;

        public string message { get; }

        public ResultStatusType status { get; }

        public static Result<T> Failure(string message = null)
        {
            return new Result<T>(ResultStatusType.Failure, message);
        }

        public static Result<T> Failure(T entity)
        {
            return new Result<T>(ResultStatusType.Failure, entity);
        }

        public static Result<T> NotFound()
        {
            return new Result<T>(ResultStatusType.NotFound);
        }

        public static Result<T> Success(T entity)
        {
            return new Result<T>(ResultStatusType.Success, entity);
        }
    }
}
