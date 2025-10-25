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

    public SignUpDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService, 
        ICommonService commonService,
        IScheduleDataService scheduleDataService,
        IVolunteerDataService volunteerDataService,
        ISmsQueueDataService smsQueueDataService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
        _scheduleDataService = scheduleDataService;
        _volunteerDataService = volunteerDataService;
        _smsQueueDataService = smsQueueDataService;
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
}




