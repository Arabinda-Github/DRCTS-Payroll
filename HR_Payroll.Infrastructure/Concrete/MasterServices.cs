using Dapper;
using HR_Payroll.Core.Model.Master;
using HR_Payroll.Core.Services;
using HR_Payroll.Infrastructure.Data;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.Data.SqlClient;
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
    public class MasterServices : IMasterServices
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MasterServices> _logger;

        public MasterServices(AppDbContext context, ILogger<MasterServices> logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Result<IEnumerable<BranchWiseUserModel>>> GetBranchWiseUsersAsync(int branchId)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@BranchId", branchId, DbType.Int32);

                var result = await connection.QueryAsync<BranchWiseUserModel>(
                    "sp_GetBranchWiseUsers",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (result == null || !result.Any())
                {
                    return Result<IEnumerable<BranchWiseUserModel>>.Failure("No data found for the specified branch.");
                }

                return Result<IEnumerable<BranchWiseUserModel>>.Success(result.ToList(), "Data retrieved successfully.");
            }
            catch (Exception ex)
            {
                // Wrap exception in a failure result
                return Result<IEnumerable<BranchWiseUserModel>>.Failure($"Error retrieving data: {ex.Message}");
            }
            finally
            {
                // Ensure connection is closed
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }

    }
}
