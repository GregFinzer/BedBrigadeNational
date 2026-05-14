using BedBrigade.Data;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Tests.Integration;

public class TestDataContextFactory : IDbContextFactory<DataContext>
{
    private readonly DbContextOptions<DataContext> _options;

    public TestDataContextFactory(DbContextOptions<DataContext> options)
    {
        _options = options;
    }

    public DataContext CreateDbContext()
    {
        return new DataContext(_options);
    }
}

