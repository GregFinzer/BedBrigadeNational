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
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<SignUp>> UpdateAsync(SignUp entity)
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

        await _smsQueueDataService.DeleteBySignUpId(existingSignup.Data.SignUpId);

        var deleteSignUpResponse = await DeleteAsync(existingSignup.Data.SignUpId);

        if (!deleteSignUpResponse.Success)
        {
            return new ServiceResponse<SignUp>($"Failed to unregister volunteer {volunteerId} from schedule {scheduleId}: {deleteSignUpResponse.Message}", false);
        }

        var scheduleResponse = await _scheduleDataService.GetByIdAsync(scheduleId);

        if (!scheduleResponse.Success || scheduleResponse.Data == null)
        {
            return new ServiceResponse<SignUp>($"Failed to retrieve schedule {scheduleId} after unregister", false);
        }

        var schedule = scheduleResponse.Data;
        if (schedule.VolunteersRegistered > 0)
        {
            schedule.VolunteersRegistered -= existingSignup.Data.NumberOfVolunteers;
        }

        if (schedule.DeliveryVehiclesRegistered > 0)
        {
            schedule.DeliveryVehiclesRegistered--;
        }

        var updateScheduleResponse = await _scheduleDataService.UpdateAsync(schedule);

        if (!updateScheduleResponse.Success)
        {
            return new ServiceResponse<SignUp>($"Failed to update schedule {scheduleId} after unregister: {updateScheduleResponse.Message}", false);
        }

        _cachingService.ClearScheduleRelated();
        return new ServiceResponse<SignUp>($"Successfully unregistered volunteer {volunteerId} from schedule {scheduleId}", true, existingSignup.Data);
    }
}



