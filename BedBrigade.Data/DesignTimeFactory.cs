using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BedBrigade.Data
{
    public class DesignTimeFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            const string connectionString =
                @"server=localhost\\sqlexpress;database=bedbrigade;trusted_connection=SSPI;Encrypt=False";
            optionsBuilder.UseSqlServer(connectionString);
            return new DataContext(optionsBuilder.Options);
        }
    }
}
