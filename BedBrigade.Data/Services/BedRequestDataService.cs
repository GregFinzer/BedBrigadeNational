using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestDataService : Repository<BedRequest>, IBedRequestDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;

    public BedRequestDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider,
        ICommonService commonService) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
    }

    public override async Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest entity)
    {
        var result = await base.CreateAsync(entity);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<BedRequest>> UpdateAsync(BedRequest entity)
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

    //We allow the National, Location Admin Plus, and National Scheduler to see all bed requests for all locations because they may move a bed request to a different location
    public async Task<ServiceResponse<List<BedRequest>>> GetAllForLocationAsync()
    {
        string? roleName = await GetUserRole();

        if (roleName == null)
            return new ServiceResponse<List<BedRequest>>("No Role found");

        int locationId = await GetUserLocationId();
        bool isAdmin = roleName.ToLower() == RoleNames.NationalAdmin.ToLower()
                       || roleName.ToLower() == RoleNames.NationalScheduler.ToLower()
                       || roleName.ToLower() == RoleNames.LocationAdminPlus.ToLower();

        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetAllForLocationAsync with LocationId,isAdmin ({locationId},{isAdmin})");
        var cachedContent = _cachingService.Get<List<BedRequest>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<BedRequest>>($"GetAllForLocationAsync Found {cachedContent.Count} {GetEntityName()} records in cache", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            if (!isAdmin)
            {
                var result = await dbSet.Where(b => b.LocationId == locationId).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<BedRequest>>($"GetAllForLocationAsync Found {result.Count()} {GetEntityName()} records", true, result);
            }

            var nationalAdminResponse = await dbSet.ToListAsync();
            _cachingService.Set(cacheKey, nationalAdminResponse);
            return new ServiceResponse<List<BedRequest>>($"GetAllForLocationAsync Found {nationalAdminResponse.Count()} {GetEntityName()} records", true, nationalAdminResponse);
        }
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmail()
    {
        return await _commonService.GetDistinctEmail(this);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId)
    {
        return await _commonService.GetDistinctEmailByLocation(this, locationId);
    }

    public async Task<ServiceResponse<List<string>>> EmailsForNotReceivedABed(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"EmailsForNotReceivedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId && o.Status != BedRequestStatus.Delivered).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> EmailsForReceivedABed(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"RecievedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId && o.Status == BedRequestStatus.Delivered).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> EmailsForSchedule(int scheduleId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"EmailsForSchedule({scheduleId})");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.ScheduleId == scheduleId).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
        }
    }
}



