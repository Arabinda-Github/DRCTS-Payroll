using Dapper;
using HR_Payroll.Core.DTO.Dept;
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

namespace HR_Payroll.Infrastructure.Concrete
{
    public class DepartmentServices : IDepartmentServices
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DepartmentServices> _logger;

        public DepartmentServices(AppDbContext context, ILogger<DepartmentServices> logger)
        {
            _logger = logger;
            _context = context;
        }

        private async Task<Result<IEnumerable<T>>> QueryWithDapperAsync<T>(string sql, DynamicParameters parameters = null)
        {
            using var connection = _context.Database.GetDbConnection();
            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var result = await connection.QueryAsync<T>(sql, parameters);

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

        public Task<Result<IEnumerable<DepartmentDTO>>> GetDepartmentsAsync() =>
         QueryWithDapperAsync<DepartmentDTO>(
             "SELECT DepartmentId, DepartmentName FROM [drconnect123].[Departments] WHERE Del_Flg='N'"
         );

    }
}
