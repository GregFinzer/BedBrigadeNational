namespace BedBrigade.Data.Services
{
    public interface IMigrationDataService
    {
        Task<bool> MigrateAsync();
        Task<bool> SeedAsync();
    }
}
