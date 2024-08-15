using BedBrigade.Common.Enums;
using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using static BedBrigade.Common.Logic.Common;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Data.Services;

public class ConfigurationDataService : Repository<Configuration>, IConfigurationDataService
{
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly AuthenticationStateProvider _auth;

    public ConfigurationDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _auth = authProvider;
    }

    /// <summary>
    /// Get all the configuration record within a given section, e.g. System, Email, Media
    /// </summary>
    /// <param name="section"></param>
    /// <returns>List of configuration records from a given section </returns>
    public async Task<ServiceResponse<List<Configuration>>> GetAllAsync(ConfigSection section)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetAllAsync({section})");
        var cachedContent = _cachingService.Get<List<Configuration>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<Configuration>>($"Found {cachedContent.Count} records in cache", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Configuration>();
            var result = await dbSet.Where(c => c.Section == section).ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<Configuration>>(result.Count + " records found", true, result);
        }
    }
}



