using Microsoft.EntityFrameworkCore;
using CCEAPI.Model;

namespace CCEAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
            public DbSet<Country> Countries { get ; set ;}
            public DbSet<RefreshMetadata> RefreshMetadata { get ; set ; }
    }
}
