using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestEstimatedWaitDataService : Repository<BedRequest>, IBedRequestEstimatedWaitDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    public BedRequestEstimatedWaitDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, IAuthService authService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
    }

    public async Task FillEstimatedWait(BedRequest bedRequest)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            bedRequest.NumberOfBedsAhead= await dbSet.Where(o => o.LocationId == bedRequest.LocationId
                                   && o.Status == BedRequestStatus.Waiting
                                   && o.CreateDate < bedRequest.CreateDate).SumAsync(o => o.NumberOfBeds);

            BedRequest? firstDeliveredBedRequest = await dbSet.Where(o => o.LocationId == bedRequest.LocationId
                                                                          && o.DeliveryDate.HasValue
                                                                          && (o.Status ==
                                                                              BedRequestStatus.Delivered ||
                                                                              o.Status == BedRequestStatus.Given))
                .FirstOrDefaultAsync(o => o.DeliveryDate == bedRequest.DeliveryDate);

            if (firstDeliveredBedRequest == null)
            {
                bedRequest.EstimatedWait = "Unknown";
                return;
            }

            TimeSpan timeSpan = DateTime.Now - bedRequest.DeliveryDate.Value;
            double monthsWeHaveDelivered =  timeSpan.TotalDays / Defaults.AverageDaysInAMonth ;
            if (monthsWeHaveDelivered >  24)
                monthsWeHaveDelivered = 24;

            DateTime targetDate = DateTime.Now.AddMonths((int)monthsWeHaveDelivered * -1);
            int delivered = await dbSet.Where(o => o.LocationId == bedRequest.LocationId
                                                                 && (o.Status == BedRequestStatus.Delivered || o.Status == BedRequestStatus.Given)
                                                                 && o.DeliveryDate > targetDate)
                .SumAsync(o => o.NumberOfBeds);

            double averageDeliveryPerMonth = delivered / monthsWeHaveDelivered;
            double estimatedMonths = bedRequest.NumberOfBedsAhead * averageDeliveryPerMonth;
            bedRequest.EstimatedWait = FormatEstimatedWaitText(estimatedMonths);
        }
    }
    
    private static string FormatEstimatedWaitText(double estimatedMonths)
    {
        if (estimatedMonths < 1.0)
        {
            double weeks = estimatedMonths * 4.345;
            int weeksRoundedUp = (int)Math.Ceiling(weeks);
            weeksRoundedUp = Math.Max(weeksRoundedUp, 1);
            return weeksRoundedUp == 1 ? "1 week" : $"{weeksRoundedUp} weeks";
        }
        else
        {
            int monthsRoundedUp = (int)Math.Ceiling(estimatedMonths);
            return monthsRoundedUp == 1 ? "1 month" : $"{monthsRoundedUp} months";
        }
    }
}