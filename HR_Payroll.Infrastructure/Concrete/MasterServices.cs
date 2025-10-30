using Dapper;
using HR_Payroll.Core.DTO.Dept;
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
     
       
    }
}
