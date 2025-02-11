using BedBrigade.Common.Enums;

using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using BedBrigade.Common.Models;
using BedBrigade.Client.Services;

namespace BedBrigade.Data.Services;

public class VolunteerDataService : Repository<Volunteer>, IVolunteerDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;

    public VolunteerDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService, ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
    }

    public override async Task<ServiceResponse<Volunteer>> CreateAsync(Volunteer entity)
    {
        var result = await base.CreateAsync(entity);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<Volunteer>> UpdateAsync(Volunteer entity)
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

    public async Task<ServiceResponse<List<Volunteer>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmail()
    {
        return await _commonService.GetDistinctEmail(this);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId)
    {
        return await _commonService.GetDistinctEmailByLocation(this, locationId);
    }

    public async Task<ServiceResponse<List<string>>> GetVolunteerEmailsWithDeliveryVehicles(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetVolunteerEmailsWithDeliveryVehicles({locationId})");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Volunteer>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                && (o.VehicleType == VehicleType.Minivan
                    || o.VehicleType == VehicleType.SUV
                    || o.VehicleType == VehicleType.Truck)).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> GetVolunteerEmailsForASchedule(int scheduleId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetVolunteerEmailsForASchedule({scheduleId})");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache",
                true, cachedContent);
        ;
        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var result = await ctx.SignUps
                    .Where(o => o.ScheduleId == scheduleId)
                    .Select(b => b.Volunteer.Email)
                    .Distinct().ToListAsync();

                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true,
                    result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<string>>(
                $"Could not GetVolunteerEmailsForASchedule {GetEntityName()}  with scheduleId {scheduleId}: {ex.Message} ({ex.ErrorCode})",
                false);
        }
    }

    public async Task<ServiceResponse<Volunteer>> GetByEmail(string email)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetByEmail({email})");
        var cachedContent = _cachingService.Get<Volunteer>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<Volunteer>($"Found GetByEmail({email}) in cache", true, cachedContent);

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Volunteer>();
                var result = await dbSet.FirstOrDefaultAsync(o => o.Email == email);

                if (result != null)
                {
                    _cachingService.Set(cacheKey, result);

                    return new ServiceResponse<Volunteer>($"Found GetByEmail({email})", true, result);
                }
                else
                {
                    return new ServiceResponse<Volunteer>($"GetByEmail({email}) not found", false);
                }
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Volunteer>(
                $"Could not GetByEmail for Volunteer {email}: {ex.Message} ({ex.ErrorCode})",
                false);
        }
    }

    public async Task<ServiceResponse<Volunteer>> GetByPhone(string phone)
    {
        return await _commonService.GetByPhone(this, phone);
    }
}



