
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using AKSoftware.Localization.MultiLanguages;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Client.Services;
using BedBrigade.SpeakIt;
using KellermanSoftware.NetEmailValidation;

namespace BedBrigade.Data.Services;

public class ScheduleDataService : Repository<Schedule>, IScheduleDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly ITranslateLogic _translateLogic;
    private readonly ILanguageContainerService _lc;

    public ScheduleDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService,
        IConfigurationDataService configurationDataService,
        ITranslateLogic translateLogic) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _configurationDataService = configurationDataService;
        _translateLogic = translateLogic;
    }

    public override Task<ServiceResponse<Schedule>> GetByIdAsync(object? id)
    {
        var result = base.GetByIdAsync(id);

        if (result.Result.Success && result.Result.Data != null)
        {
            FillSingleEventSelect(result.Result.Data);
            return result;
        }

        return result;
    }

    public override async Task<ServiceResponse<Schedule>> CreateAsync(Schedule entity)
    {
        var result = await base.CreateAsync(entity);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<Schedule>> UpdateAsync(Schedule entity)
    {
        var result = await base.UpdateAsync(entity);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<bool>> DeleteAsync(object id)
    {
        var result = await base.DeleteAsync(id);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public async Task<ServiceResponse<List<Schedule>>> GetAvailableSchedulesByLocationId(int locationId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetAvailableLocationEvents({locationId})");
        List<Schedule>? cachedContent = _cachingService.Get<List<Schedule>>(cacheKey);

        if (cachedContent != null)
        {
            FillEventSelects(cachedContent);
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
                FillEventSelects(result);
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

    private void FillEventSelects(List<Schedule> schedules)
    {
        foreach (var schedule in schedules.ToList())
        {
            FillSingleEventSelect(schedule);
        }
    }

    private void FillSingleEventSelect(Schedule schedule)
    {
        string? eventName = _translateLogic.GetTranslation(schedule.EventName);
        schedule.EventSelect = $"{eventName}: {schedule.EventDateScheduled.ToShortDateString()}, {schedule.EventDateScheduled.ToShortTimeString()}";
    }

    public async Task<ServiceResponse<List<Schedule>>> GetSchedulesByLocationId(int locationId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetSchedulesByLocationId({locationId})");
        List<Schedule>? cachedContent = _cachingService.Get<List<Schedule>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Schedule>>($"Found {cachedContent.Count()} GetSchedulesByLocationId in cache",
                true, cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Schedule>();
                var result = await dbSet
                    .Where(o => o.LocationId == locationId).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Schedule>>($"Found {result.Count()} {GetEntityName()}", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<Schedule>>(
                $"Error GetSchedulesByLocationId for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
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



