using System.Data.Common;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestFailedDeliveryDataService : Repository<BedRequest>, IBedRequestFailedDeliveryDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    public BedRequestFailedDeliveryDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, IAuthService authService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
    }
    


    public async Task<ServiceResponse<List<BedRequest>>> GetReplacementBedRequests(BedRequest failedBedRequest)
    {
        try
        {
            var waitingBedRequestsResponse = await GetWaitingForLocation(failedBedRequest.LocationId);

            if (!waitingBedRequestsResponse.Success || waitingBedRequestsResponse.Data == null)
                return waitingBedRequestsResponse;

            List<BedRequest> result = waitingBedRequestsResponse.Data.Where(o =>
                o.BedRequestId != failedBedRequest.BedRequestId
                && o.NumberOfBeds <= failedBedRequest.NumberOfBeds
                && (o.Notes == null || !o.Notes.Contains(Defaults.FailedDeliveryText))).ToList();

            result = SortByNumberOfBedsAndDistance(failedBedRequest, result);
            return new ServiceResponse<List<BedRequest>>("Found for ScheduleId", true, result);

        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<BedRequest>>(
                $"Could not GetReplacementBedRequests {GetEntityName()}  with failedBedRequest {failedBedRequest.BedRequestId}: {ex.Message} ({ex.ErrorCode})",
                false);
        }
    }
    
    public async Task<ServiceResponse<List<BedRequest>>> GetWaitingForLocation(int locationId)
    {
        try
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetWaitingForLocation({locationId})");
            var cachedContent = _cachingService.Get<List<BedRequest>>(cacheKey);
            if (cachedContent != null)
                return new ServiceResponse<List<BedRequest>>(
                    $"Found {cachedContent.Count} {GetEntityName()} records in cache for GetWaitingForLocation", true,
                    cachedContent);
            ;
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<BedRequest>();
                var result = await dbSet.Where(o => o.LocationId == locationId
                                                    && o.Status == BedRequestStatus.Waiting)
                    .OrderBy(o => o.CreateDate).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<BedRequest>>($"Found {result.Count} {GetEntityName()} records", true,
                    result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<BedRequest>>(
                $"Could not GetWaitingForLocation {GetEntityName()}  with locationId {locationId}: {ex.Message} ({ex.ErrorCode})",
                false);
        }
    }
    
    private List<BedRequest> SortByNumberOfBedsAndDistance(BedRequest failedBedRequest, List<BedRequest> bedRequests)
    {
        if (failedBedRequest.Latitude == null && failedBedRequest.Longitude == null)
        {
            return bedRequests.OrderByDescending(o => o.NumberOfBeds)
                .ThenBy(o => o.CreateDate).ToList();
        }

        //Fill distance
        foreach (var bedRequest in bedRequests)
        {
            if (failedBedRequest.Latitude == null && failedBedRequest.Longitude == null)
                bedRequest.Distance = Defaults.DefaultDistance;
                
            bedRequest.Distance = DriveRoutingLogic.CalculateDistanceInMiles((double)failedBedRequest.Latitude.Value, 
                (double)failedBedRequest.Longitude.Value,
                (double)bedRequest.Latitude, 
                (double)bedRequest.Longitude);
        }

        return bedRequests
            .OrderByDescending(o => o.NumberOfBeds)
            .OrderBy(o => o.Distance).ToList();
    }
}