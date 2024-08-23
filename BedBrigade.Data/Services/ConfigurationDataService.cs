using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

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

    public override async Task<ServiceResponse<Configuration>> GetByIdAsync(object? id)
    {
        if (id == null)
        {
            return new ServiceResponse<Configuration>("Configuration.GetByIdAsync id is null", false);
        }
        var response = await base.GetAllAsync();

        if (!response.Success || response.Data == null)
            return new ServiceResponse<Configuration>(response.Message, false);

        Configuration result = response.Data.FirstOrDefault(o => o.ConfigurationKey == id.ToString());

        if (result == null)
        {
            return new ServiceResponse<Configuration>("Configuration.GetByIdAsync not found: " + id);
        }

        return new ServiceResponse<Configuration>("Configuration.GetByIdAsyncFound: " + id, true, result);
    }

    public async Task<int> GetConfigValueAsIntAsync(ConfigSection section, string key)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;

        var config = configs.FirstOrDefault(c => c.ConfigurationKey == key);

        if (config == null)
            ThrowKeyNotFound(section, key);

        if (!int.TryParse(config.ConfigurationValue, out int result))
        {
            throw new FormatException($"Configuration value is not an integer for {section} - {key}");
        }

        return result;
    }

    public void ThrowKeyNotFound(ConfigSection section, string key)
    {
        throw new KeyNotFoundException($"Configuration not found for {section} - {key}");
    }

    public async Task<string> GetConfigValueAsync(ConfigSection section, string key)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;

        var config = configs.FirstOrDefault(c => c.ConfigurationKey == key);

        if (config == null)
            ThrowKeyNotFound(section, key);

        return config.ConfigurationValue;
    }

    public async Task<decimal> GetConfigValueAsDecimalAsync(ConfigSection section, string key)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;

        var config = configs.FirstOrDefault(c => c.ConfigurationKey == key);

        if (config == null)
            ThrowKeyNotFound(section, key);

        if (!decimal.TryParse(config.ConfigurationValue, out decimal result))
        {
            throw new FormatException($"Configuration value is not an decimal for {section} - {key}");
        }

        return result;
    }

    public async Task<bool> GetConfigValueAsBoolAsync(ConfigSection section, string key)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;

        var config = configs.FirstOrDefault(c => c.ConfigurationKey == key);

        if (config == null)
            ThrowKeyNotFound(section, key);

        return config.ConfigurationValue.ToLower() == "true";
    }
}



