using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Data.Common;
using Twilio.TwiML.Voice;

namespace BedBrigade.Data.Services;

public class SignUpDataService : Repository<SignUp>, ISignUpDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;
    private readonly IScheduleDataService _scheduleDataService;
    private readonly IVolunteerDataService _volunteerDataService;
    private readonly ISmsQueueDataService _smsQueueDataService;
    private readonly ITimezoneDataService _timezoneDataService;
    public SignUpDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService, 
        ICommonService commonService,
        IScheduleDataService scheduleDataService,
        IVolunteerDataService volunteerDataService,
        ISmsQueueDataService smsQueueDataService,
        ITimezoneDataService timezoneDataService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
        _scheduleDataService = scheduleDataService;
        _volunteerDataService = volunteerDataService;
        _smsQueueDataService = smsQueueDataService;
        _timezoneDataService = timezoneDataService;
    }

    public override async Task<ServiceResponse<SignUp>> CreateAsync(SignUp entity)
    {
        var result = await base.CreateAsync(entity);
        await _scheduleDataService.UpdateScheduleVolunteers(entity.ScheduleId);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<SignUp>> UpdateAsync(SignUp entity)
    {
        var result = await base.UpdateAsync(entity);
        await _scheduleDataService.UpdateScheduleVolunteers(entity.ScheduleId);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<bool>> DeleteAsync(object id)
    {
        await _smsQueueDataService.DeleteBySignUpId((int) id);
        var existingResponse = await GetByIdAsync(id);

        var result = await base.DeleteAsync(id);

        if (existingResponse.Success && existingResponse.Data != null)
        {
            await _scheduleDataService.UpdateScheduleVolunteers(existingResponse.Data.ScheduleId);
        }

        _cachingService.ClearScheduleRelated();
        return result;
    }

    public async Task<ServiceResponse<SignUp>> GetByVolunteerEmailAndScheduleId(int volunteerId, int scheduleId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetByVolunteerEmailAndScheduleId({volunteerId},{scheduleId})");
        SignUp? cachedContent = _cachingService.Get<SignUp>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<SignUp>($"Found GetByVolunteerEmailAndScheduleId({volunteerId},{scheduleId}) in cache",
                true, cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<SignUp>();
                var result = await dbSet.FirstOrDefaultAsync(o => o.VolunteerId == volunteerId && o.ScheduleId == scheduleId);

                if (result != null)
                {
                    _cachingService.Set(cacheKey, result);
                    return new ServiceResponse<SignUp>($"Found GetByVolunteerEmailAndScheduleId({volunteerId},{scheduleId})", true, result);
                }

                return new ServiceResponse<SignUp>($"Could not find GetByVolunteerEmailAndScheduleId({volunteerId},{scheduleId})", false);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<SignUp>($"Could not GetByVolunteerEmailAndScheduleId({volunteerId},{scheduleId}): {ex.Message} ({ex.ErrorCode})", false);
        }
    }

    public async Task<ServiceResponse<SignUp>> Unregister(string volunteerEmail, int scheduleId)
    {
        var volunteerResponse = await _volunteerDataService.GetByEmail(volunteerEmail);

        if (!volunteerResponse.Success || volunteerResponse.Data == null)
        {
            return new ServiceResponse<SignUp>($"Volunteer with email {volunteerEmail} not found", false);
        }

        int volunteerId = volunteerResponse.Data.VolunteerId;

        var existingSignup = await GetByVolunteerEmailAndScheduleId(volunteerId, scheduleId);

        if (!existingSignup.Success || existingSignup.Data == null)
        {
            return new ServiceResponse<SignUp>($"No sign-up found for volunteer {volunteerId} and schedule {scheduleId}", false);
        }

        var deleteSignUpResponse = await DeleteAsync(existingSignup.Data.SignUpId);

        if (!deleteSignUpResponse.Success)
        {
            return new ServiceResponse<SignUp>($"Failed to unregister volunteer {volunteerId} from schedule {scheduleId}: {deleteSignUpResponse.Message}", false);
        }

        _cachingService.ClearScheduleRelated();
        return new ServiceResponse<SignUp>($"Successfully unregistered volunteer {volunteerId} from schedule {scheduleId}", true, existingSignup.Data);
    }

    public async Task<ServiceResponse<List<SignUp>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }

    public async Task<ServiceResponse<List<SignUp>>> GetSignUpsForDashboard(int locationId)
    {
        // Calculate target date to include the next two Saturdays
        DateTime today = DateTime.UtcNow.Date;
        int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        DateTime nextSaturday = today.AddDays(daysUntilSaturday == 0 ? 0 : daysUntilSaturday);
        DateTime secondSaturday = nextSaturday.AddDays(7);
        DateTime targetDateInclusive = secondSaturday.Date.AddDays(1).AddTicks(-1); // end of the day

        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), locationId, $"GetSignUpsForDashboard({targetDateInclusive:yyyyMMdd})");
        List<SignUp>? cachedContent = _cachingService.Get<List<SignUp>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<SignUp>>($"Found {cachedContent.Count} GetSignUpsForDashboard in cache", true, cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var result = await ctx.SignUps
                    .Include(s => s.Schedule)
                    .Include(s => s.Volunteer)
                    .Where(s => s.LocationId == locationId
                                && s.Schedule != null
                                && s.Schedule.EventDateScheduled >= today
                                && s.Schedule.EventDateScheduled <= targetDateInclusive)
                    .OrderByDescending(s => s.UpdateDate)
                    .ToListAsync();

                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<SignUp>>($"Found {result.Count} {GetEntityName()} for dashboard through {secondSaturday:yyyy-MM-dd}", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<SignUp>>($"Error GetSignUpsForDashboard for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
        }
    }

    public async Task<ServiceResponse<List<SignUpDisplayItem>>> GetSignUpsForSignUpGrid(int locationId, string filter)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), locationId, $"GetSignUpsForSignUpGrid({locationId}, {filter})");
        List<SignUpDisplayItem>? cachedContent = _cachingService.Get<List<SignUpDisplayItem>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<SignUpDisplayItem>>($"Found {cachedContent.Count} GetSignUpsForSignUpGrid in cache", true, cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                IQueryable<SignUpDisplayItem> query = BuildSignUpGridQuery(locationId, ctx);

                query = ApplySignUpGridFilter(filter, query);

                var result = await query.OrderBy(item => item.ScheduleEventDate).ToListAsync();
                _timezoneDataService.FillLocalDates(result);
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<SignUpDisplayItem>>($"Found {result.Count} SignUps for SignUpGrid", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<SignUpDisplayItem>>($"Error GetSignUpsForSignUpGrid for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
        }
    }

    private static IQueryable<SignUpDisplayItem> BuildSignUpGridQuery(int locationId, DataContext ctx)
    {
        var query = from sch in ctx.Schedules
            join loc in ctx.Locations
                on sch.LocationId equals loc.LocationId
            join su in ctx.SignUps
                on sch.ScheduleId equals su.ScheduleId into suGroup
            from su in suGroup.DefaultIfEmpty() // Left join
            join vol in ctx.Volunteers
                on su.VolunteerId equals vol.VolunteerId into volGroup
            from vol in volGroup.DefaultIfEmpty() // Left join
            where sch.LocationId == locationId
            select new SignUpDisplayItem
            {
                ScheduleId = sch.ScheduleId,
                SignUpId = su == null ? 0 : su.SignUpId,
                ScheduleLocationId = sch.LocationId,
                ScheduleLocationName = loc.Name,
                ScheduleEventName = sch.EventName,
                ScheduleEventDate = sch.EventDateScheduled,
                ScheduleEventType = sch.EventType,
                SignUpNumberOfVolunteers = su == null ? 0 : su.NumberOfVolunteers,
                VolunteerId = vol == null ? 0 : vol.VolunteerId,
                VolunteerFirstName = vol == null ? string.Empty : vol.FirstName,
                VolunteerLastName = vol == null ? string.Empty : vol.LastName,
                VolunteerEmail = vol == null ? string.Empty : vol.Email,
                VolunteerPhone = vol == null ? string.Empty : vol.Phone,
                VolunteerOrganization = vol == null ? string.Empty : vol.Organization,
                VehicleType = su == null ? null : su.VehicleType,
                SignUpNote = su == null ?  null : su.SignUpNote,
                CreateDate = su == null ? DateTime.Now : su.CreateDate,
                IHaveVolunteeredBefore = vol != null && vol.IHaveVolunteeredBefore
            };
        return query;
    }

    private static IQueryable<SignUpDisplayItem> ApplySignUpGridFilter(string filter, IQueryable<SignUpDisplayItem> query)
    {
        switch (filter)
        {
            case "reg":
                query = query.Where(item => item.SignUpId != 0 && item.ScheduleEventDate.Date >= DateTime.UtcNow.Date);
                break;
            case "notreg":
                query = query.Where(item => item.SignUpId == 0 && item.ScheduleEventDate.Date >= DateTime.UtcNow.Date);
                break;
            case "allfuture":
                query = query.Where(item => item.ScheduleEventDate.Date >= DateTime.UtcNow.Date);
                break;
            case "allpast":
                query = query.Where(item => item.ScheduleEventDate.Date < DateTime.UtcNow.Date);
                break;
            default:
                throw new ArgumentException($"Invalid filter for GetSignUpsForSignUpGrid: {filter}");
        }

        return query;
    }

    public async Task<ServiceResponse<List<Volunteer>>> GetVolunteersNotSignedUpForAnEvent(int locationId, int scheduleId)
    {
        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var signedUpVolunteerIds = await ctx.SignUps
                    .Where(su => su.ScheduleId == scheduleId)
                    .Select(su => su.VolunteerId)
                    .ToListAsync();
                var volunteersNotSignedUp = await ctx.Volunteers
                    .Where(v => v.LocationId == locationId && !signedUpVolunteerIds.Contains(v.VolunteerId))
                    .ToListAsync();
                return new ServiceResponse<List<Volunteer>>($"Found {volunteersNotSignedUp.Count} volunteers not signed up for schedule {scheduleId}", true, volunteersNotSignedUp);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<Volunteer>>($"Error retrieving volunteers not signed up for schedule {scheduleId}: {ex.Message} ({ex.ErrorCode})", false, null);
        }
    }
}




