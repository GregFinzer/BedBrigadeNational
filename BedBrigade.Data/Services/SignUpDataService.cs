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

    public SignUpDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService, ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
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

    public async Task<ServiceResponse<List<SignUp>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
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
}



