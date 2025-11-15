using Dapper;
using HR_Payroll.Core.DTO.Dept;
using HR_Payroll.Core.Model.DataTable;
using HR_Payroll.Core.Model.Master;
using HR_Payroll.Core.Services;
using HR_Payroll.Infrastructure.Data;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

        private async Task<Result<T>> QuerySingleWithDapperAsync<T>(string sql, DynamicParameters parameters = null)
        {
            using var connection = _context.Database.GetDbConnection();
            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var result = await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, commandType: CommandType.StoredProcedure);

                if (result == null)
                    return Result<T>.Failure($"{typeof(T).Name} not found.");

                return Result<T>.Success(result, $"{typeof(T).Name} retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"Error retrieving {typeof(T).Name}: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }

        private async Task<Result<IEnumerable<T>>> QueryWithDapperAsync<T>(string sql,DynamicParameters? parameters = null,CommandType commandType = CommandType.Text)
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

        public Task<Result<IEnumerable<DepartmentDTO>>> GetDepartmentsAsync() =>
         QueryWithDapperAsync<DepartmentDTO>(
             "SELECT DepartmentId, DepartmentName FROM [dbo].[Departments] WHERE Del_Flg='N'"
         );

        public async Task<Result<DepartmentAssignResult>> AssignDepartmentHierarchyAsync(DepartmentAssignDTO dto)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@DepartmentId", dto.DepartmentId);
            parameters.Add("@SubDepartmentId", dto.SubDepartmentId);
            parameters.Add("@ManagerId", dto.ManagerId);
            parameters.Add("@TeamLeadId", dto.TeamLeadId);
            parameters.Add("@EmployeeId", dto.EmployeeId);
            parameters.Add("@CreatedBy", dto.CreatedBy);
            parameters.Add("@Remarks", dto.Remarks);

            string sql = "dbo.AssignDepartmentHierarchy";

            return await QuerySingleWithDapperAsync<DepartmentAssignResult>(sql, parameters);
        }

        public async Task<Result<IEnumerable<AssignEmployeeListModel>>> GetAssignListAsync(PaginationDataRequestModel model)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Start", model.Start);
            parameters.Add("@Length", model.Length);
            parameters.Add("@Search", model.Search);
            parameters.Add("@SortColumn", model.SortColumn);
            parameters.Add("@SortDirection", model.SortDirection);

            return await QueryWithDapperAsync<AssignEmployeeListModel>(
                "sp_GetAssignedEmployees",
                parameters,
                CommandType.StoredProcedure
            );
        }

    }
}
