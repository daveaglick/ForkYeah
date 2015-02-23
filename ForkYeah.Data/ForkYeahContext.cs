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
        public ForkYeahContext() : base("ForkYeahConntextionString")
        {
        }
        
        public DbSet<Repository> Repositories { get; set; }
    }
}
