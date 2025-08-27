using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BedBrigade.Data.Services;

public class ConfigurationDataService : Repository<Configuration>, IConfigurationDataService
{
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly IAuthService _authService;
    private readonly ICommonService _commonService;
    public ConfigurationDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService, ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _authService = authService;
        _commonService = commonService;
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
            return new ServiceResponse<List<Configuration>>($"Found {cachedContent.Count} records in cache", true,
                cachedContent);
        ;

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

        Configuration result = response.Data.FirstOrDefault(o =>
            o.ConfigurationKey == id.ToString() && o.LocationId == Defaults.NationalLocationId);

        if (result == null)
        {
            return new ServiceResponse<Configuration>("Configuration.GetByIdAsync not found: " + id);
        }

        return new ServiceResponse<Configuration>("Configuration.GetByIdAsyncFound: " + id, true, result);
    }

    public async Task<int> GetConfigValueAsIntAsync(ConfigSection section, string key)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;

        var config =
            configs.FirstOrDefault(c => c.ConfigurationKey == key && c.LocationId == Defaults.NationalLocationId);

        if (config == null)
            ThrowKeyNotFound(section, key, Defaults.NationalLocationId);

        if (!int.TryParse(config.ConfigurationValue, out int result))
        {
            throw new FormatException($"Configuration value is not an integer for {section} - {key}");
        }

        return result;
    }

    public void ThrowKeyNotFound(ConfigSection section, string key, int locationId)
    {
        throw new KeyNotFoundException($"Configuration not found for {section} - {key}");
    }

    public async Task<string> GetConfigValueAsync(ConfigSection section, string key)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;

        var config =
            configs.FirstOrDefault(c => c.ConfigurationKey == key && c.LocationId == Defaults.NationalLocationId);

        if (config == null)
            ThrowKeyNotFound(section, key, Defaults.NationalLocationId);

        return config.ConfigurationValue;
    }

    public async Task<string> GetConfigValueAsync(ConfigSection section, string key, int locationId)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;
        var config = configs.FirstOrDefault(c => c.ConfigurationKey == key && c.LocationId == locationId);
        if (config == null)
            throw new KeyNotFoundException($"Configuration not found for {section} - {key} for location {locationId}");

        return config.ConfigurationValue;
    }

    public async Task<decimal> GetConfigValueAsDecimalAsync(ConfigSection section, string key)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;

        var config =
            configs.FirstOrDefault(c => c.ConfigurationKey == key && c.LocationId == Defaults.NationalLocationId);

        if (config == null)
            ThrowKeyNotFound(section, key, Defaults.NationalLocationId);

        if (!decimal.TryParse(config.ConfigurationValue, out decimal result))
        {
            throw new FormatException($"Configuration value is not an decimal for {section} - {key}");
        }

        return result;
    }

    public async Task<bool> GetConfigValueAsBoolAsync(ConfigSection section, string key)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;

        var config =
            configs.FirstOrDefault(c => c.ConfigurationKey == key && c.LocationId == Defaults.NationalLocationId);

        if (config == null)
            ThrowKeyNotFound(section, key, Defaults.NationalLocationId);

        return config.ConfigurationValue.ToLower() == "true" || config.ConfigurationValue.ToLower() == "yes";
    }

    public async Task<List<decimal>> GetAmounts(ConfigSection section, string key, int locationId)
    {
        List<Configuration> configs = (await GetAllAsync(section)).Data;
        var config = configs.FirstOrDefault(c => c.ConfigurationKey == key && c.LocationId == locationId);
        if (config == null)
            ThrowKeyNotFound(section, key, locationId);
        var amounts = config.ConfigurationValue.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
                decimal.TryParse(s.Trim(), out decimal value)
                    ? value
                    : throw new FormatException($"Invalid decimal value: {s}"))
            .ToList();
        return amounts;
    }

    public override async Task<ServiceResponse<Configuration>> UpdateAsync(Configuration entity)
    {
        ServiceResponse<Configuration> updateResult;

        if (entity.ConfigurationKey == ConfigNames.IsCachingEnabled)
        {
            updateResult = await base.UpdateAsync(entity);

            if (updateResult.Success)
            {
                bool previousValue = _cachingService.IsCachingEnabled;
                _cachingService.IsCachingEnabled = entity.ConfigurationValue.ToLower() == "true"
                                                   || entity.ConfigurationValue.ToLower() == "yes";

                Log.Information($"Caching enabled went from {previousValue} to {_cachingService.IsCachingEnabled}");
                _cachingService.ForceClearAll();
            }
        }
        else
        {
            updateResult = await base.UpdateAsync(entity);
        }

        return updateResult;
    }

    public async Task<ServiceResponse<List<Configuration>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }

}



