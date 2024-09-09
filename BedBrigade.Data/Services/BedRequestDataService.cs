using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestDataService : Repository<BedRequest>, IBedRequestDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;
    private readonly ILocationDataService _locationDataService;

    public BedRequestDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider,
        ICommonService commonService,
        ILocationDataService locationDataService) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
        _locationDataService = locationDataService;
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

    public async Task<ServiceResponse<List<BedRequest>>> GetAllForLocationAsync(int locationId)
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

    public async Task<ServiceResponse<List<string>>> EmailsForNotReceivedABed(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"EmailsForNotReceivedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for EmailsForNotReceivedABed", true, cachedContent); ;

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
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for EmailsForReceivedABed", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId && o.Status == BedRequestStatus.Delivered).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> EmailsForSchedule(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"EmailsForSchedule({locationId})");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for EmailsForSchedule", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId && o.Status == BedRequestStatus.Scheduled).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetAllForLocationList(List<int> locationIds)
    {
        return await _commonService.GetAllForLocationList(this, locationIds);
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetScheduledBedRequestsForLocation(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"ScheduledBedRequestsForLocation({locationId})");
        var cachedContent = _cachingService.Get<List<BedRequest>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<BedRequest>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for GetScheduledBedRequestsForLocation", true, cachedContent);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId 
                                                && o.Status == BedRequestStatus.Scheduled).ToListAsync();

            result = await SortScheduledBedRequests(locationId, result);
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<BedRequest>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    private async Task<List<BedRequest>> SortScheduledBedRequests(int locationId, List<BedRequest> bedRequests)
    {
        var locationResult = await _locationDataService.GetByIdAsync(locationId);

        if (!locationResult.Success || locationResult.Data == null || !Validation.IsValidZipCode(locationResult.Data.PostalCode))
        {
            return bedRequests.OrderBy(o => o.TeamNumber).ThenBy(o => o.PostalCode).ToList();
        }

        var location = locationResult.Data;
        var addressParser = LibraryFactory.CreateAddressParser();

        foreach (var bedRequest in bedRequests)
        {
            bedRequest.Distance = 0;
            if (location.PostalCode != bedRequest.PostalCode && Validation.IsValidZipCode(bedRequest.PostalCode))
            {
                bedRequest.Distance = addressParser.GetDistanceInMilesBetweenTwoZipCodes(location.PostalCode, bedRequest.PostalCode);
            }
        }

        return bedRequests.OrderBy(o => o.TeamNumber).ThenBy(o => o.Distance).ThenBy(o => o.CreateDate).ToList();
    }

}



