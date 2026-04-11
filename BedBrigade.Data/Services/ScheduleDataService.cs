using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using AKSoftware.Localization.MultiLanguages;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.SpeakIt;


namespace BedBrigade.Data.Services;

public class ScheduleDataService : Repository<Schedule>, IScheduleDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly ITranslateLogic _translateLogic;
    private readonly ILanguageContainerService _lc;
    private readonly ILocationDataService _locationDataService;
    
    public ScheduleDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService,
        IConfigurationDataService configurationDataService,
        ITranslateLogic translateLogic, 
        ILocationDataService locationDataService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _configurationDataService = configurationDataService;
        _translateLogic = translateLogic;
        _locationDataService = locationDataService;
    }

    public override async Task<ServiceResponse<Schedule>> GetByIdAsync(object? id)
    {
        var result = await base.GetByIdAsync(id);

        if (result.Success && result.Data != null)
        {
            FillSingleEventSelect(result.Data);
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

    public async Task<ServiceResponse<List<Schedule>>> GetScheduleForMonthsAndLocation(int locationId, int numberOfMonthsAway)
    {
        // Retrieve available schedules for the specified location.
        var availableSchedulesResponse = await GetAvailableSchedulesByLocationId(locationId);
        if (!availableSchedulesResponse.Success || availableSchedulesResponse.Data == null)
        {
            return availableSchedulesResponse;
        }

        // Calculate the upper bound date by adding the specified number of months to the first day of the month
        DateTime upperBoundDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(numberOfMonthsAway);

        // Filter schedules to include only those whose EventDateScheduled is within the desired range.
        var filteredSchedules = availableSchedulesResponse.Data
            .Where(schedule => schedule.EventDateScheduled <= upperBoundDate)
            .ToList();

        return new ServiceResponse<List<Schedule>>(
            $"Found {filteredSchedules.Count} schedules within {numberOfMonthsAway} month(s)",
            true,
            filteredSchedules);
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

    public async Task<ServiceResponse<Schedule?>> GetLastScheduledByLocationId(int locationId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetLastScheduledByLocationId({locationId})");
        Schedule? cachedContent = _cachingService.Get<Schedule>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<Schedule>($"Found GetLastScheduledByLocationId in cache", true, cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Schedule>();
                var result = await dbSet
                    .Where(o => o.LocationId == locationId)
                    .OrderByDescending(o => o.EventDateScheduled).FirstOrDefaultAsync();

                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<Schedule>($"GetLastScheduledByLocationId", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Schedule>(
                $"Error GetLastScheduledByLocationId for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
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

    public async Task<ServiceResponse<List<Schedule>>> GetPastSchedulesByLocationId(int locationId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetPastSchedulesByLocationId({locationId})");
        List<Schedule>? cachedContent = _cachingService.Get<List<Schedule>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Schedule>>($"Found {cachedContent.Count()} GetPastSchedulesByLocationId in cache",
                true, cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Schedule>();
                var result = await dbSet
                    .Where(o => o.LocationId == locationId && o.EventDateScheduled.Date < DateTime.UtcNow.Date)
                    .OrderBy(o => o.EventDateScheduled).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Schedule>>($"Found {result.Count()} {GetEntityName()}", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<Schedule>>(
                $"Error GetPastSchedulesByLocationId for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
        }
    }

    public async Task UpdateBedRequestSummaryInformation(int locationId, List<BedRequest> scheduledBedRequests)
    {
        var scheduleResult = await GetFutureSchedulesByLocationId(locationId);
        if (!scheduleResult.Success || scheduleResult.Data == null || scheduleResult.Data.Count == 0)
            return;

        foreach (var schedule in scheduleResult.Data)
        {
            if (scheduledBedRequests.Any(o =>
                    o.DeliveryDate.HasValue && o.DeliveryDate.Value.Date == schedule.EventDateScheduled.Date))
            {
                schedule.Teams = scheduledBedRequests
                    .Where(o => o.DeliveryDate.HasValue &&
                                o.DeliveryDate.Value.Date == schedule.EventDateScheduled.Date)
                    .Select(request => request.Team)
                    .Distinct()
                    .Count();

                schedule.Beds = scheduledBedRequests
                    .Where(o => o.DeliveryDate.HasValue &&
                                o.DeliveryDate.Value.Date == schedule.EventDateScheduled.Date)
                    .Sum(request => request.NumberOfBeds);

                await UpdateAsync(schedule);
            }
        }
    }

    // Updates VolunteersRegistered and DeliveryVehiclesRegistered for a specific schedule
    public async Task UpdateScheduleVolunteers(int scheduleId)
    {

        using (var ctx = _contextFactory.CreateDbContext())
        {

            // Calculate totals from SignUps for this schedule
            var signUps = await ctx.Set<SignUp>().Where(s => s.ScheduleId == scheduleId).ToListAsync();

            int volunteersRegistered = signUps
                .Select(s => (int?)s.NumberOfVolunteers)
                .Sum() ?? 0;

            int deliveryVehiclesRegistered = signUps
                .Count(s => s.VehicleType != VehicleType.None);

            // Load the schedule and update summary fields
            var scheduleResponse = await GetByIdAsync(scheduleId);
            if (!scheduleResponse.Success || scheduleResponse.Data == null)
            {
                return;
            }

            var schedule = scheduleResponse.Data;
            schedule.VolunteersRegistered = volunteersRegistered;
            schedule.DeliveryVehiclesRegistered = deliveryVehiclesRegistered;

            await UpdateAsync(schedule);
        }
    }

    public async Task<ServiceResponse<Schedule>> GetScheduleForBedRequestDeliveryDate(BedRequest bedRequest)
    {
        if (!bedRequest.DeliveryDate.HasValue)
            return new ServiceResponse<Schedule>("bedRequest.DeliveryDate is null");

        var scheduleResponse = await GetFutureSchedulesByLocationId(bedRequest.LocationId);

        if (!scheduleResponse.Success || scheduleResponse.Data == null)
            return new ServiceResponse<Schedule>(scheduleResponse.Message);

        var schedule = scheduleResponse.Data
            .FirstOrDefault(o => o.EventDateScheduled.Date == bedRequest.DeliveryDate.Value.Date
            && o.EventType == EventType.Delivery);

        if (schedule != null)
            return new ServiceResponse<Schedule>("Found schedule", true, schedule);

        return new ServiceResponse<Schedule>(
            $"Schedule not found with delivery date of {bedRequest.DeliveryDate.Value.ToShortDateString()}. Please add a Schedule for that date.");
    }

    public async Task<ServiceResponse<Schedule>> AddMissingScheduleForBedRequestDeliveryDate(BedRequest bedRequest)
    {
        if (!bedRequest.DeliveryDate.HasValue)
            return  new ServiceResponse<Schedule>("BedRequest DeliveryDate is null");
        
        var locationResponse = await _locationDataService.GetByIdAsync(bedRequest.LocationId);
        
        if (!locationResponse.Success || locationResponse.Data == null)
            return new ServiceResponse<Schedule>(locationResponse.Message);
        
        Schedule schedule = new  Schedule();
        schedule.LocationId = bedRequest.LocationId;
        schedule.EventName = "Delivery";
        schedule.EventType = EventType.Delivery;
        schedule.EventStatus = EventStatus.Scheduled;
        schedule.EventDateScheduled = bedRequest.DeliveryDate.Value.Date;
        schedule.Address = locationResponse.Data.BuildAddress;
        schedule.City = locationResponse.Data.BuildCity;
        schedule.State = locationResponse.Data.BuildState;
        schedule.PostalCode = locationResponse.Data.BuildPostalCode;

        var lastScheduleResponse = await GetLastScheduledByLocationId(bedRequest.LocationId);

        if (lastScheduleResponse.Success && lastScheduleResponse.Data != null)
        {
            schedule.OrganizerName = lastScheduleResponse.Data.OrganizerName;
            schedule.OrganizerEmail = lastScheduleResponse.Data.OrganizerEmail;
            schedule.OrganizerPhone = lastScheduleResponse.Data.OrganizerPhone.FormatPhoneNumber();
        }
        else //This will only happen if there is no previous schedule
        {
            schedule.OrganizerName = StringUtil.InsertSpaces(GetUserName());
            schedule.OrganizerEmail = GetUserEmail();
            schedule.OrganizerPhone = GetUserPhone().FormatPhoneNumber();
        }

        int defaultHour = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.Schedule,
            ConfigNames.DefaultDeliveryTime, schedule.LocationId);
        schedule.EventDateScheduled = schedule.EventDateScheduled.AddHours(defaultHour);
        int defaultDuration = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.Schedule,
            ConfigNames.DefaultDeliveryDurationHours, schedule.LocationId);
        schedule.EventDurationHours = defaultDuration;
        int defaultMaxVolunteers = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.Schedule,
            ConfigNames.DefaultDeliveryMaxVolunteers, schedule.LocationId);
        schedule.VolunteersMax = defaultMaxVolunteers;
        string defaultEventNote = await _configurationDataService.GetConfigValueAsync(ConfigSection.Schedule,
            ConfigNames.DefaultDeliveryEventNote, schedule.LocationId);
        schedule.EventNote = defaultEventNote;
        schedule.VolunteersRegistered = 0;
        schedule.DeliveryVehiclesRegistered = 0;
        schedule.Teams = 0;
        schedule.Beds = 0;
        var createResponse = await CreateAsync(schedule);
        if (!createResponse.Success || createResponse.Data == null)
            return new ServiceResponse<Schedule>(createResponse.Message);

        return new ServiceResponse<Schedule>("Success", true, createResponse.Data);
    }
    

}




