using HR_Payroll.Core.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        //private readonly IDomainEventDispatcher? _dispatcher;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            //_dispatcher = dispatcher;
        }

        public DbSet<sp_UserLogin> sp_UserLogin => Set<sp_UserLogin>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
           
            return result;
        }

        public override int SaveChanges() =>
              SaveChangesAsync().GetAwaiter().GetResult();
    }
}
