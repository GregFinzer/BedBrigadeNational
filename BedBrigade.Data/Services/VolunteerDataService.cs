using BedBrigade.Common.Enums;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class VolunteerDataService : Repository<Volunteer>, IVolunteerDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;
    private readonly IVolunteerEventsDataService _volunteerEventsDataService;

    public VolunteerDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider, ICommonService commonService,
        IVolunteerEventsDataService volunteerEventsDataService) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
        _volunteerEventsDataService = volunteerEventsDataService;
    }

    public async Task<ServiceResponse<List<Volunteer>>> GetAllForLocationAsync()
    {
        return await _commonService.GetAllForLocationAsync(this);
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
                var result = await ctx.VolunteerEvents
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
}



