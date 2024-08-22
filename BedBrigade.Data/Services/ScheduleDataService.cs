using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;

namespace BedBrigade.Data.Services;

public class ScheduleDataService : Repository<Schedule>, IScheduleDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly IConfigurationDataService _configurationDataService;

    public ScheduleDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        AuthenticationStateProvider authProvider,
        IConfigurationDataService configurationDataService) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _configurationDataService = configurationDataService;
    }

    public async Task<ServiceResponse<List<Schedule>>> GetAvailableSchedulesByLocationId(int locationId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetAvailableLocationEvents({locationId})");
        List<Schedule>? cachedContent = _cachingService.Get<List<Schedule>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Schedule>>($"Found {cachedContent.Count()} GetAvailableLocationEvents in cache",
                true, cachedContent);
        }

        int eventCutOffTimeDays;

        try
        {
            eventCutOffTimeDays =
                await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                    ConfigNames.EventCutOffTimeDays);
        }
        catch (Exception ex)
        {
            return new ServiceResponse<List<Schedule>>($"{ex.Message}", false, null);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Schedule>();
                var result = await dbSet
                    .Where(o => o.LocationId == locationId 
                                && o.EventStatus == EventStatus.Scheduled
                                && o.EventDateScheduled.Date >= DateTime.Today.AddDays(eventCutOffTimeDays)
                                && (o.VolunteersMax == 0 || o.VolunteersRegistered < o.VolunteersMax))
                    .OrderBy(o => o.EventDateScheduled).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Schedule>>($"Found {result.Count()} {GetEntityName()}", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<Schedule>>(
                $"Error GetFutureSchedulesByLocationId for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
        }
    }

    public async Task<ServiceResponse<List<Schedule>>> GetFutureSchedulesByLocationId(int locationId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetFutureSchedulesByLocationId({locationId})");
        List<Schedule>? cachedContent = _cachingService.Get<List<Schedule>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Schedule>>($"Found {cachedContent.Count()} GetFutureSchedulesByLocationId in cache",
                true, cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Schedule>();
                var result = await dbSet
                    .Where(o => o.LocationId == locationId && o.EventDateScheduled.Date >= DateTime.UtcNow.Date)
                    .OrderBy(o => o.EventDateScheduled).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Schedule>>($"Found {result.Count()} {GetEntityName()}", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<Schedule>>(
                $"Error GetFutureSchedulesByLocationId for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
        }
    }
}



