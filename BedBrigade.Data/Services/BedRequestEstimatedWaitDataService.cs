using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestEstimatedWaitDataService : Repository<BedRequest>, IBedRequestEstimatedWaitDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    
    public BedRequestEstimatedWaitDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService, 
        IAuthService authService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
    }

    public async Task<ServiceResponse<string>> GetEstimatedWaitTime(int locationId)
    {
        EstimatedWaitResult estimatedWaitResult = await GetEstimatedWaitResult(locationId, Defaults.SqlServerMinDate);
        return new ServiceResponse<string>("Estimated wait", true, estimatedWaitResult.EstimatedWait);
    }

    public async Task<EstimatedWaitResult> GetEstimatedWaitResult(int locationId, DateTime maximumBedRequestDate)
    {
        maximumBedRequestDate = maximumBedRequestDate.Date;
        string cacheKey = _cachingService.BuildCacheKey(nameof(BedRequest),
            $"GetEstimatedWaitResult({locationId},{maximumBedRequestDate.ToShortDateString()})");
        
        var cached = _cachingService.Get<EstimatedWaitResult>(cacheKey);
        if (cached != null)
            return cached;
        
        EstimatedWaitResult estimatedWaitResult = new EstimatedWaitResult();
        estimatedWaitResult.LocationId = locationId;
        
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            await FillNumberOfWaitingBedRequests(dbSet, maximumBedRequestDate, estimatedWaitResult);
            await FillFirstDeliveryDate(dbSet, estimatedWaitResult);

            //No deliveries so just return
            if (!estimatedWaitResult.FirstDeliveryDate.HasValue)
            {
                _cachingService.Set(cacheKey, estimatedWaitResult);
                return estimatedWaitResult;
            }

            await FillLastDeliveryDate(dbSet, estimatedWaitResult);
            await FillNumberOfDeliveredBedRequests(dbSet, estimatedWaitResult);
            FillAverageDeliveriesPerDay(estimatedWaitResult);
            FillEstimatedWait(estimatedWaitResult);
            _cachingService.Set(cacheKey, estimatedWaitResult);
            return estimatedWaitResult;
        }
    }

    private void FillEstimatedWait(EstimatedWaitResult estimatedWaitResult)
    {
        if (estimatedWaitResult.AverageDeliveriesPerDay == 0)
        {
            estimatedWaitResult.EstimatedWait = "Unknown";
            return;
        }

        double estimatedWaitInDays = ((double) estimatedWaitResult.NumberOfWaitingBedRequests) /
                                     estimatedWaitResult.AverageDeliveriesPerDay;

        if (estimatedWaitInDays > Defaults.AverageDaysInAMonth)
        {
            double estimatedMonths = estimatedWaitInDays / Defaults.AverageDaysInAMonth;
            int monthsRoundedUp = (int)Math.Ceiling(estimatedMonths);
            estimatedWaitResult.EstimatedWait = monthsRoundedUp <= 1 ? "1 month" : $"{monthsRoundedUp} months";
        }
        else if (estimatedWaitInDays > 7)
        {
            double estimatedWeeks = estimatedWaitInDays / ((double) 7);
            int weeksRoundedUp = (int)Math.Ceiling(estimatedWeeks);
            estimatedWaitResult.EstimatedWait = weeksRoundedUp  <= 1 ? "1 week" : $"{weeksRoundedUp} weeks";
        }
        else
        {
            int daysRoundedUp = (int)Math.Ceiling(estimatedWaitInDays);
            estimatedWaitResult.EstimatedWait = daysRoundedUp  <= 1 ? "1 day" : $"{daysRoundedUp} days";
        }
    }
    
    private async Task FillNumberOfDeliveredBedRequests(DbSet<BedRequest> dbSet, EstimatedWaitResult estimatedWaitResult)
    {
        estimatedWaitResult.NumberOfDeliveredBedRequests = await dbSet.Where(o => o.LocationId == estimatedWaitResult.LocationId
                && (o.Status == BedRequestStatus.Delivered
                    || o.Status == BedRequestStatus.Given)
                && o.DeliveryDate.HasValue
                && o.DeliveryDate.Value.Date >= estimatedWaitResult.FirstDeliveryDate)
            .SumAsync(o => o.NumberOfBeds);
    }

    private async Task FillLastDeliveryDate(DbSet<BedRequest> dbSet, EstimatedWaitResult estimatedWaitResult)
    {
        estimatedWaitResult.LastDeliveryDate = await dbSet.Where(o => o.LocationId == estimatedWaitResult.LocationId
                                                                      && (o.Status == BedRequestStatus.Delivered
                                                                          || o.Status == BedRequestStatus.Given))
            .OrderByDescending(o => o.DeliveryDate)
            .Select(o => o.DeliveryDate)
            .FirstOrDefaultAsync();
    }

    private async Task FillFirstDeliveryDate(DbSet<BedRequest> dbSet, EstimatedWaitResult estimatedWaitResult)
    {
        //For averaging purposes we only look back a maximum of 24 months
        const int MaxMonths = 24;
        DateTime minimumDeliveryDate = DateTime.UtcNow.AddMonths(-MaxMonths);
        estimatedWaitResult.FirstDeliveryDate = await dbSet.Where(o => o.LocationId == estimatedWaitResult.LocationId
                                                                       && (o.Status == BedRequestStatus.Delivered
                                                                           || o.Status == BedRequestStatus.Given)
                                                                       && o.DeliveryDate.HasValue
                                                                       && o.DeliveryDate.Value.Date >= minimumDeliveryDate)
            .OrderBy(o => o.DeliveryDate)
            .Select(o => o.DeliveryDate)
            .FirstOrDefaultAsync();
    }

    private static async Task FillNumberOfWaitingBedRequests(DbSet<BedRequest> dbSet, 
        DateTime maximumBedRequestDate,
        EstimatedWaitResult estimatedWaitResult)
    {
        if (maximumBedRequestDate <= Defaults.SqlServerMinDate)
        {
            estimatedWaitResult.NumberOfWaitingBedRequests =  await dbSet
                .Where(o => o.LocationId == estimatedWaitResult.LocationId
                            && o.Status == BedRequestStatus.Waiting )
                .SumAsync(o => o.NumberOfBeds);
        }
        else
        {
            estimatedWaitResult.NumberOfWaitingBedRequests =  await dbSet
                .Where(o => o.LocationId == estimatedWaitResult.LocationId
                            && o.Status == BedRequestStatus.Waiting
                            && o.CreateDate.HasValue
                            && o.CreateDate.Value.Date <= maximumBedRequestDate)
                .SumAsync(o => o.NumberOfBeds);
        }

    }

    private void FillAverageDeliveriesPerDay(EstimatedWaitResult estimatedWaitResult)
    {
        if (estimatedWaitResult.NumberOfDeliveredBedRequests == 0
            || estimatedWaitResult.NumberOfWaitingBedRequests == 0
            || !estimatedWaitResult.FirstDeliveryDate.HasValue
            || !estimatedWaitResult.LastDeliveryDate.HasValue
            || estimatedWaitResult.FirstDeliveryDate.Value.Date == estimatedWaitResult.LastDeliveryDate.Value.Date)
        {
            return;
        }

        TimeSpan timeSpan = estimatedWaitResult.LastDeliveryDate.Value - estimatedWaitResult.FirstDeliveryDate.Value;

        if (timeSpan.TotalDays > 0)
        {
            estimatedWaitResult.AverageDeliveriesPerDay =
                ((double)estimatedWaitResult.NumberOfDeliveredBedRequests) / timeSpan.TotalDays;
        }
    }
    
    public async Task FillEstimatedWait(BedRequest bedRequest)
    {
        EstimatedWaitResult estimatedWaitResult = await GetEstimatedWaitResult(bedRequest.LocationId, bedRequest.DeliveryDate.Value);
        bedRequest.EstimatedWait = estimatedWaitResult.EstimatedWait;
    }
    

}