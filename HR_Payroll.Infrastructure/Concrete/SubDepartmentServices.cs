using Dapper;
using HR_Payroll.Core.DTO.Dept;
using HR_Payroll.Core.Model.Master;
using HR_Payroll.Core.Services;
using HR_Payroll.Infrastructure.Data;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace HR_Payroll.Infrastructure.Concrete
{
    public class SubDepartmentServices : ISubDepartmentServices
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SubDepartmentServices> _logger;

        public SubDepartmentServices(AppDbContext context, ILogger<SubDepartmentServices> logger)
        {
            _logger = logger;
            _context = context;
        }

        private async Task<Result<IEnumerable<T>>> QueryWithDapperAsync<T>(string sql, DynamicParameters parameters = null, CommandType commandType = CommandType.Text)
        {
            using var connection = _context.Database.GetDbConnection();
            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var result = await connection.QueryAsync<T>(sql, parameters, commandType: commandType);

                if (result == null || !result.Any())
                    return Result<IEnumerable<T>>.Failure($"No {typeof(T).Name}s found.");

                return Result<IEnumerable<T>>.Success(result.ToList(), $"{typeof(T).Name}s retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<T>>.Failure($"Error retrieving {typeof(T).Name}s: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }

        public Task<Result<IEnumerable<SubDepartmentDTO>>> GetSubDepartmentAsync(int departmentId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@DepartmentId", departmentId, DbType.Int32);

            return QueryWithDapperAsync<SubDepartmentDTO>(
                "SELECT SubDepartmentId, DepartmentId, SubDepartmentName FROM [dbo].[SubDepartments] " +
                "WHERE Del_Flg='N' AND (DepartmentId=@DepartmentId OR @DepartmentId=0)",
                parameters
            );
        }

        public Task<Result<IEnumerable<BranchWiseUserModel>>> GetBranchWiseUsersAsync(int subDepartmentId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BranchId", subDepartmentId, DbType.Int32);

            return QueryWithDapperAsync<BranchWiseUserModel>(
                "sp_GetBranchWiseUsers",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

    }
}
