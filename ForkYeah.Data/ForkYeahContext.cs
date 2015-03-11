using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForkYeah.Data
{
    public class ForkYeahContext : DbContext
    {    
        public ForkYeahContext() : base("ForkYeahConnectionString")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ForkYeahContext, Migrations.Configuration>());
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Repository> Repositories { get; set; }
        public DbSet<UpdateHistory> UpdateHistories { get; set; }
    }
}
