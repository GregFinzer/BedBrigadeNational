using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Syncfusion.XlsIO;

namespace BedBrigade.Data.Services;

public class DashboardDataService : IDashboardDataService
{
    private const string BedRequestEntityName = nameof(BedRequest);

    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ILocationDataService _locationDataService;

    public DashboardDataService(
        IDbContextFactory<DataContext> contextFactory,
        ICachingService cachingService,
        ILocationDataService locationDataService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _locationDataService = locationDataService;
    }

    public async Task<ServiceResponse<List<BedRequestDashboardRow>>> GetWaitingDashboard(int userLocationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(BedRequestEntityName, $"GetWaitingDashboard({userLocationId})");
        var cachedContent = _cachingService.Get<List<BedRequestDashboardRow>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<BedRequestDashboardRow>>(
                $"Found {cachedContent.Count} {BedRequestEntityName} records in cache for GetWaitingDashboard", true,
                cachedContent);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();

            var grouped = await dbSet
                .Where(o => o.Status == BedRequestStatus.Waiting)
                .GroupBy(o => o.LocationId)
                .Select(g => new BedRequestDashboardRow
                {
                    LocationId = g.Key,
                    LocationName = ctx.Set<Location>().First(l => l.LocationId == g.Key).Name,
                    Requests = g.Count(),
                    Beds = g.Sum(x => x.NumberOfBeds)
                })
                .ToListAsync();

            var ordered = grouped
                .OrderByDescending(r => r.LocationId == userLocationId)
                .ThenBy(r => r.LocationName)
                .ToList();

            ordered.Add(new BedRequestDashboardRow
            {
                LocationName = "Total",
                Requests = ordered.Sum(r => r.Requests),
                Beds = ordered.Sum(r => r.Beds)
            });

            _cachingService.Set(cacheKey, ordered);
            return new ServiceResponse<List<BedRequestDashboardRow>>($"Found {ordered.Count} {BedRequestEntityName} records", true,
                ordered);
        }
    }

    public async Task<ServiceResponse<List<BedRequestHistoryRow>>> GetBedRequestHistory(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(BedRequestEntityName, $"GetBedRequestHistory({locationId})");
        var cachedContent = _cachingService.Get<List<BedRequestHistoryRow>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<BedRequestHistoryRow>>(
                $"Found {cachedContent.Count} {BedRequestEntityName} records in cache for GetBedRequestHistory", true,
                cachedContent);
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var now = DateTime.UtcNow;
            var currentYear = now.Year;
            var years = new int[] { currentYear - 2, currentYear - 1, currentYear };
            List<BedRequestHistoryRow> result;

            if (locationId == Defaults.GroveCityLocationId)
            {
                result = await GetBedRequestHistoryForGroveCity(locationId, dbSet, years, now);
            }
            else
            {
                result = await dbSet
                    .Where(o => o.LocationId == locationId && o.CreateDate.HasValue &&
                                years.Contains(o.CreateDate.Value.Year) && o.CreateDate.Value <= now)
                    .GroupBy(o => new { Year = o.CreateDate.Value.Year, Month = o.CreateDate.Value.Month })
                    .Select(g => new BedRequestHistoryRow
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Beds = g.Sum(x => x.NumberOfBeds),
                        Requests = g.Count()
                    })
                    .ToListAsync();
            }

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<BedRequestHistoryRow>>($"Found {result.Count} {BedRequestEntityName} records",
                true, result);
        }
    }

    private static async Task<List<BedRequestHistoryRow>> GetBedRequestHistoryForGroveCity(int locationId, DbSet<BedRequest> dbSet, int[] years, DateTime now)
    {
        List<BedRequestHistoryRow> result;
        result = await dbSet
            .Where(o => o.LocationId == locationId
                        && o.Group != Defaults.GroupPeace
                        && o.Group != Defaults.GroupUalc
                        && o.CreateDate.HasValue
                        && years.Contains(o.CreateDate.Value.Year)
                        && o.CreateDate.Value <= now)
            .GroupBy(o => new { Year = o.CreateDate.Value.Year, Month = o.CreateDate.Value.Month })
            .Select(g => new BedRequestHistoryRow
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Beds = g.Sum(x => x.NumberOfBeds),
                Requests = g.Count()
            })
            .ToListAsync();
        return result;
    }

    public async Task<ServiceResponse<List<BedRequestHistoryRow>>> GetBedDeliveryHistory(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(BedRequestEntityName, $"GetBedDeliveryHistory({locationId})");
        var cachedContent = _cachingService.Get<List<BedRequestHistoryRow>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<BedRequestHistoryRow>>(
                $"Found {cachedContent.Count} {BedRequestEntityName} records in cache for GetBedDeliveryHistory", true,
                cachedContent);
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var now = DateTime.UtcNow;
            var query = BuildDeliveryHistoryQuery(ctx.Set<BedRequest>(), locationId, now);

            var result = await query
                .GroupBy(o => new { Year = o.DeliveryDate.Value.Year, Month = o.DeliveryDate.Value.Month })
                .Select(g => new BedRequestHistoryRow
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Beds = g.Sum(x => x.NumberOfBeds),
                    Requests = g.Count()
                })
                .ToListAsync();

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<BedRequestHistoryRow>>($"Found {result.Count} {BedRequestEntityName} records",
                true, result);
        }
    }

    public async Task<ServiceResponse<string>> GetEstimatedWaitTime(int locationId)
    {
        string cacheKey = GetWaitTimeCacheKey(locationId);
        var cached = _cachingService.Get<string>(cacheKey);
        if (!string.IsNullOrWhiteSpace(cached))
            return new ServiceResponse<string>("Found cached estimated wait time", true, cached);

        var (success, bedsWaiting) = await TryGetBedsWaitingAsync(locationId);
        if (!success)
            return new ServiceResponse<string>("Insufficient data to estimate wait time", false, null);

        if (bedsWaiting <= 0)
        {
            _cachingService.Set(cacheKey, "0 weeks");
            return new ServiceResponse<string>("No beds waiting", true, "0 weeks");
        }

        var deliveries = await GetDeliveriesHistoryAsync(locationId);
        if (deliveries == null)
            return new ServiceResponse<string>("Insufficient data to estimate wait time", false, null);

        double avgDelPerMonth = CalculateAverageDeliveredPerMonth(deliveries);
        if (avgDelPerMonth <= 0)
            return new ServiceResponse<string>("Insufficient data to estimate wait time", true, "0 weeks");

        double estimatedMonths = bedsWaiting / avgDelPerMonth;
        string resultText = FormatEstimatedWaitText(estimatedMonths);
        _cachingService.Set(cacheKey, resultText);
        return new ServiceResponse<string>("Estimated wait time calculated", true, resultText);
    }

    public async Task<ServiceResponse<List<NationalDelivery>>> GetNationalDeliveries()
    {
        var now = DateTime.UtcNow;
        int currentYear = now.Year;
        int prevYear = currentYear - 1;
        int twoYearsAgo = currentYear - 2;

        string cacheKey = _cachingService.BuildCacheKey(BedRequestEntityName, $"GetNationalDeliveries()");
        var cachedContent = _cachingService.Get<List<NationalDelivery>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<NationalDelivery>>($"Found {cachedContent.Count} {BedRequestEntityName} records in cache for GetNationalDeliveries", true, cachedContent);
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var delivered = await (from br in ctx.BedRequests
                                   join loc in ctx.Locations on br.LocationId equals loc.LocationId
                                   where br.DeliveryDate.HasValue
                                         && br.DeliveryDate!.Value <= now
                                         && (br.Status == BedRequestStatus.Delivered || br.Status == BedRequestStatus.Given)
                                   select new
                                   {
                                       Location = loc.Name,
                                       Group = br.Group,
                                       Year = br.DeliveryDate!.Value.Year,
                                       Beds = br.NumberOfBeds
                                   }).ToListAsync();

            var results = new List<NationalDelivery>();

            var currentYtd = delivered.Where(x => x.Year == currentYear).ToList();
            string currentYearLabel = $"{currentYear} YTD";
            AddGroupedRows(currentYtd, currentYearLabel, 10, results);

            var prev = delivered.Where(x => x.Year == prevYear).ToList();
            AddGroupedRows(prev, prevYear.ToString(), 20, results);

            var twoAgo = delivered.Where(x => x.Year == twoYearsAgo).ToList();
            AddGroupedRows(twoAgo, twoYearsAgo.ToString(), 30, results);

            var older = delivered.Where(x => x.Year < twoYearsAgo).ToList();
            AddGroupedRows(older, "Older", 40, results);

            AddNationalDeliveryYtdTotal(currentYtd, results, currentYear, currentYearLabel);
            AddNationalDeliveryPreviousTotal(prev, results, prevYear);
            AddNationalDeliveryTwoYearsAgoTotal(twoAgo, results, twoYearsAgo);
            AddNationalDeliveryOlderTotal(older, results);
            AddNationalDeliveryGrandTotal(delivered, results);

            results = results
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Location)
                .ThenBy(r => r.Group)
                .ToList();

            _cachingService.Set(cacheKey, results);

            return new ServiceResponse<List<NationalDelivery>>($"Found {results.Count} national delivery rows", true,
                results);
        }
    }

    public async Task<ServiceResponse<List<DeliveryPlan>>> GetDeliveryPlan(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(BedRequestEntityName, $"GetDeliveryPlan({locationId})");
        var cachedContent = _cachingService.Get<List<DeliveryPlan>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<DeliveryPlan>>($"Found {cachedContent.Count} {BedRequestEntityName} records in cache for GetDeliveryPlan", true,
                cachedContent);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();

            var query = await dbSet
                .Where(o => o.LocationId == locationId && o.Status == BedRequestStatus.Scheduled && o.DeliveryDate.HasValue)
                .GroupBy(o => new { Date = o.DeliveryDate.Value.Date, Group = o.Group ?? string.Empty, Team = o.Team ?? string.Empty })
                .Select(g => new DeliveryPlan
                {
                    DeliveryDate = g.Key.Date,
                    Group = g.Key.Group,
                    Team = g.Key.Team,
                    NumberOfBeds = g.Sum(x => x.NumberOfBeds),
                    Stops = g.Count()
                })
                .OrderBy(d => d.DeliveryDate)
                .ThenBy(d => d.Group)
                .ThenBy(d => d.Team)
                .ToListAsync();

            _cachingService.Set(cacheKey, query);
            return new ServiceResponse<List<DeliveryPlan>>($"Found {query.Count} delivery plan rows", true, query);
        }
    }



    private string GetWaitTimeCacheKey(int locationId)
        => _cachingService.BuildCacheKey(BedRequestEntityName, $"GetEstimatedWaitTime({locationId})");

    private async Task<(bool Success, int BedsWaiting)> TryGetBedsWaitingAsync(int locationId)
    {
        var dashboard = await GetWaitingDashboard(locationId);
        if (!dashboard.Success || dashboard.Data == null)
            return (false, 0);

        int bedsWaiting = dashboard.Data.FirstOrDefault(r => r.LocationId == locationId)?.Beds ?? 0;
        return (true, bedsWaiting);
    }

    private async Task<List<BedRequestHistoryRow>?> GetDeliveriesHistoryAsync(int locationId)
    {
        var deliveriesResponse = await GetBedDeliveryHistory(locationId);
        if (!deliveriesResponse.Success || deliveriesResponse.Data == null)
            return null;
        return deliveriesResponse.Data;
    }

    private static double CalculateAverageDeliveredPerMonth(List<BedRequestHistoryRow> data)
    {
        var currentYear = DateTime.UtcNow.Year;
        var currentMonth = DateTime.UtcNow.Month;
        int prevYear = currentYear - 1;

        int[] delPrevYear = new int[12];
        int[] delCurrentYtd = new int[currentMonth];

        foreach (var row in data.Where(r => r.Year == prevYear))
        {
            if (row.Month >= 1 && row.Month <= 12) delPrevYear[row.Month - 1] = row.Beds;
        }

        foreach (var row in data.Where(r => r.Year == currentYear))
        {
            if (row.Month >= 1 && row.Month <= currentMonth) delCurrentYtd[row.Month - 1] = row.Beds;
        }

        return AverageExcludingZeros(delPrevYear.Concat(delCurrentYtd));
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

    private static double AverageExcludingZeros(IEnumerable<int> values)
    {
        var nonZero = values.Where(v => v > 0).ToList();
        if (nonZero.Count == 0) return 0.0;
        return nonZero.Average();
    }

    private static IQueryable<BedRequest> BuildDeliveryHistoryQuery(IQueryable<BedRequest> source, int locationId,
        DateTime now)
    {
        int currentYear = now.Year;
        var years = new int[] { currentYear - 2, currentYear - 1, currentYear };

        var query = source.Where(o => o.LocationId == locationId
                                      && o.DeliveryDate.HasValue
                                      && years.Contains(o.DeliveryDate.Value.Year)
                                      && (o.Status == BedRequestStatus.Delivered || o.Status == BedRequestStatus.Given)
                                      && o.DeliveryDate.Value <= now);

        if (locationId == Defaults.GroveCityLocationId)
        {
            query = query.Where(o => o.Group != Defaults.GroupPeace && o.Group != Defaults.GroupUalc);
        }

        return query;
    }

    private void AddNationalDeliveryGrandTotal(IEnumerable<dynamic> delivered, List<NationalDelivery> results)
    {
        var totalBeds = delivered.Sum(x => (int)x.Beds);
        if (totalBeds > 0)
        {
            results.Add(new NationalDelivery
            {
                Location = "Grand Total",
                Group = string.Empty,
                YearString = string.Empty,
                Beds = totalBeds,
                SortOrder = 999
            });
        }
    }

    private static void AddNationalDeliveryOlderTotal(IEnumerable<dynamic> older, List<NationalDelivery> results)
    {
        var totalBeds = older.Sum(x => (int)x.Beds);
        if (totalBeds > 0)
        {
            results.Add(new NationalDelivery
            {
                Location = "Older Total",
                Group = string.Empty,
                YearString = "Older",
                Beds = totalBeds,
                SortOrder = 49
            });
        }
    }

    private static void AddNationalDeliveryTwoYearsAgoTotal(IEnumerable<dynamic> twoAgo,
        List<NationalDelivery> results, int twoYearsAgo)
    {
        var totalBeds = twoAgo.Sum(x => (int)x.Beds);
        if (totalBeds > 0)
        {
            results.Add(new NationalDelivery
            {
                Location = $"{twoYearsAgo} Total",
                Group = string.Empty,
                YearString = twoYearsAgo.ToString(),
                Beds = totalBeds,
                SortOrder = 39
            });
        }
    }

    private static void AddNationalDeliveryPreviousTotal(IEnumerable<dynamic> prev,
        List<NationalDelivery> results, int prevYear)
    {
        var totalBeds = prev.Sum(x => (int)x.Beds);
        if (totalBeds > 0)
        {
            results.Add(new NationalDelivery
            {
                Location = $"{prevYear} Total",
                Group = string.Empty,
                YearString = prevYear.ToString(),
                Beds = totalBeds,
                SortOrder = 29
            });
        }
    }

    private static void AddNationalDeliveryYtdTotal(IEnumerable<dynamic> currentYtd,
        List<NationalDelivery> results, int currentYear, string currentYearLabel)
    {
        var totalBeds = currentYtd.Sum(x => (int)x.Beds);
        if (totalBeds > 0)
        {
            results.Add(new NationalDelivery
            {
                Location = $"{currentYear} YTD Total",
                Group = string.Empty,
                YearString = currentYearLabel,
                Beds = totalBeds,
                SortOrder = 19
            });
        }
    }

    private static void AddGroupedRows(IEnumerable<dynamic> source, string yearLabel, int sortBase,
        List<NationalDelivery> results)
    {
        var grouped = source
            .GroupBy(x => new { x.Location, Group = x.Group ?? string.Empty })
            .Select(g => new NationalDelivery
            {
                Location = g.Key.Location,
                Group = g.Key.Group,
                YearString = yearLabel,
                Beds = g.Sum(x => (int)x.Beds),
                SortOrder = sortBase
            });
        results.AddRange(grouped);
    }
}
