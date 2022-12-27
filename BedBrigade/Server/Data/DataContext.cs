using Microsoft.EntityFrameworkCore;
using BedBrigade.Shared;

namespace BedBrigade.Server.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Configuration> Configurations { get; set; }
    }
}
