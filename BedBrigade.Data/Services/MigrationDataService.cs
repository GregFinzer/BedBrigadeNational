using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BedBrigade.Data.Services
{
    public class MigrationDataService : IMigrationDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;

        public MigrationDataService(IDbContextFactory<DataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<bool> MigrateAsync()
        {
            Log.Logger.Information("Beginning MigrationDataService.MigrateAsync");
            using var context = _contextFactory.CreateDbContext();
            await context.Database.MigrateAsync();
            Log.Logger.Information("Finished MigrationDataService.MigrateAsync");
            return true;
        }

        public async Task<bool> SeedAsync()
        {
            Log.Logger.Information("Beginning MigrationDataService.SeedAsync");
            await BedBrigade.Data.Seeding.Seed.SeedData(_contextFactory);
            Log.Logger.Information("Finished MigrationDataService.SeedAsync");
            return true;
        }
    }
}
