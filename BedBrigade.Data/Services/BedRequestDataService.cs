using AngleSharp.Dom;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using KellermanSoftware.AddressParser;
using KellermanSoftware.NetEmailValidation;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Terminal;
using Location = BedBrigade.Common.Models.Location;

namespace BedBrigade.Data.Services;

public class BedRequestDataService : Repository<BedRequest>, IBedRequestDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;
    private readonly ILocationDataService _locationDataService;
    private readonly IGeoLocationQueueDataService _geoLocationQueueDataService;
    private readonly ITimezoneDataService _timezoneDataService;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IScheduleDataService _scheduleDataService;

    public BedRequestDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService,
        ICommonService commonService,
        ILocationDataService locationDataService,
        IGeoLocationQueueDataService geoLocationQueueDataService, 
        ITimezoneDataService timezoneDataService,
        IConfigurationDataService configurationDataService,
        IScheduleDataService scheduleDataService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
        _locationDataService = locationDataService;
        _geoLocationQueueDataService = geoLocationQueueDataService;
        _timezoneDataService = timezoneDataService;
        _configurationDataService = configurationDataService;
        _scheduleDataService = scheduleDataService;
    }

    public override async Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest entity)
    {
        var result = await base.CreateAsync(entity);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<BedRequest>> UpdateAsync(BedRequest entity)
    {
        var previousBedRequest = await GetByIdAsync(entity.BedRequestId);

        if (!previousBedRequest.Success || previousBedRequest.Data == null)
        {
            return new ServiceResponse<BedRequest>($"BedRequest with BedRequestId {entity.BedRequestId} not found", false, null);
        }

        bool geoLocationUpdateNeeded = !entity.Latitude.HasValue
                                       || !entity.Longitude.HasValue
                                       || previousBedRequest.Data.Street != entity.Street
                                       || previousBedRequest.Data.City != entity.City
                                       || previousBedRequest.Data.State != entity.State
                                       || previousBedRequest.Data.PostalCode != entity.PostalCode;

        var result = await base.UpdateAsync(entity);
        _cachingService.ClearScheduleRelated();

        var tasks = new List<Task>();

        if (geoLocationUpdateNeeded)
        {
            tasks.Add(QueueForGeoLocation(entity));
        }

        var scheduled = await GetScheduledBedRequestsForLocation(entity.LocationId);
        if (scheduled.Success && scheduled.Data != null)
        {
            tasks.Add(_scheduleDataService.UpdateBedRequestSummaryInformation(
                entity.LocationId, scheduled.Data));
        }

        // Run both in parallel for performance
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }

        return result;
    }

    private async Task QueueForGeoLocation(BedRequest bedRequest)
    {
        GeoLocationQueue item = new GeoLocationQueue();
        item.Street = bedRequest.Street;
        item.City = bedRequest.City;
        item.State = bedRequest.State;
        item.PostalCode = bedRequest.PostalCode;
        item.CountryCode = Defaults.CountryCode;
        item.TableName = TableNames.BedRequests.ToString();
        item.TableId = bedRequest.BedRequestId;
        item.QueueDate = DateTime.UtcNow;
        item.Priority = 1;
        item.Status = GeoLocationStatus.Queued.ToString();
        await _geoLocationQueueDataService.CreateAsync(item);
    }


    public override async Task<ServiceResponse<bool>> DeleteAsync(object id)
    {
        var result = await base.DeleteAsync(id);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetAllForLocationAsync(int locationId)
    {
        var result = await _commonService.GetAllForLocationAsync(this, locationId);

        if (!result.Success || result.Data == null)
        {
            return result;
        }

        _timezoneDataService.FillLocalDates(result.Data);

        return result;
    }



    public async Task<ServiceResponse<List<string>>> GetDistinctEmail()
    {
        return await _commonService.GetDistinctEmail(this);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId)
    {
        return await _commonService.GetDistinctEmailByLocation(this, locationId);
    }

    public async Task<ServiceResponse<int>> SumBedsForNotReceived(int locationId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var sum = await dbSet.Where(o => o.LocationId == locationId
                                             && o.Status == BedRequestStatus.Waiting)
                .SumAsync(b => b.NumberOfBeds);

            return new ServiceResponse<int>($"Found sum of {sum} beds", true, sum);
        }
    }

    public async Task<ServiceResponse<BedRequest>> GetByPhone(string phone)
    {
        return await _commonService.GetByPhone(this, phone);
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
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && !string.IsNullOrEmpty(o.Email)
                                                && o.Status == BedRequestStatus.Waiting).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
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
            var result = await dbSet.Where(o => o.LocationId == locationId && !string.IsNullOrEmpty(o.Email)
                                                && (o.Status == BedRequestStatus.Delivered || o.Status == BedRequestStatus.Given)).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
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
            var result = await dbSet.Where(o => o.LocationId == locationId 
                                                && !string.IsNullOrEmpty(o.Email)
                                                && o.Status == BedRequestStatus.Scheduled).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetAllForLocationList(List<int> locationIds)
    {
        var result = await _commonService.GetAllForLocationList(this, locationIds);

        if (!result.Success || result.Data == null)
        {
            return result;
        }

        _timezoneDataService.FillLocalDates(result.Data);

        return result;
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

    public List<BedRequest> SortBedRequestClosestToAddress(List<BedRequest> bedRequests, int bedRequestId)
    {
        var addressParser = LibraryFactory.CreateAddressParser();
        var targetBedRequest = bedRequests.FirstOrDefault(b => b.BedRequestId == bedRequestId);

        if (targetBedRequest == null)
            return bedRequests;

        var (targetLatitude, targetLongitude) = GetTargetCoordinates(targetBedRequest, addressParser);
        if (targetLatitude == null || targetLongitude == null)
            return bedRequests;

        foreach (var bedRequest in bedRequests)
        {
            SetDistance(bedRequest, targetBedRequest, targetLatitude.Value, targetLongitude.Value, addressParser);
        }

        return bedRequests.OrderBy(o => o.Distance).ThenBy(o => o.CreateDate).ToList();
    }

    public async Task<ServiceResponse<BedRequest>> GetWaitingByEmail(string email)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var bedRequest = await dbSet.FirstOrDefaultAsync(o => o.Email == email && o.Status == BedRequestStatus.Waiting);
            if (bedRequest == null)
            {
                return new ServiceResponse<BedRequest>($"No waiting BedRequest found for email {email}", false, null);
            }
            return new ServiceResponse<BedRequest>($"Found waiting BedRequest for email {email}", true, bedRequest);
        }
    }

    public async Task<int> CancelWaitingForBouncedEmail(List<string> emailList)
    {
        string userName = GetUserName() ?? Defaults.DefaultUserNameAndEmail;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var lowerEmailList = emailList.Select(e => e.ToLower()).ToList();

            int updated = await ctx.Set<BedRequest>()
                .Where(o => lowerEmailList.Contains(o.Email.ToLower())
                            && o.Status == BedRequestStatus.Waiting)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(o => o.UpdateUser, userName)
                    .SetProperty(o => o.UpdateDate, DateTime.UtcNow)
                    .SetProperty(o => o.MachineName, Environment.MachineName)
                    .SetProperty(o => o.Status, o => BedRequestStatus.Cancelled)
                    .SetProperty(o => o.Notes,
                        o => (o.Notes ?? "") + " | Cancelled due to bounced email"));

            if (updated > 0)
            {
                _cachingService.ClearScheduleRelated();
            }

            return updated;
        }
    }



    public async Task<ServiceResponse<BedRequest>> GetWaitingByPhone(string phone)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            string phoneWithNumbersOnly = StringUtil.ExtractDigits(phone);
            string formattedPhone = phoneWithNumbersOnly.FormatPhoneNumber();

            var dbSet = ctx.Set<BedRequest>();
            var bedRequest = await dbSet.FirstOrDefaultAsync(o => o.Status == BedRequestStatus.Waiting && (o.Phone == phoneWithNumbersOnly || o.Phone == formattedPhone));
            if (bedRequest == null)
            {
                return new ServiceResponse<BedRequest>($"No waiting BedRequest found for phone {phone}", false, null);
            }
            return new ServiceResponse<BedRequest>($"Found waiting BedRequest for phone {phone}", true, bedRequest);
        }
    }

    private (double? Latitude, double? Longitude) GetTargetCoordinates(BedRequest request, AddressParser addressParser)
    {
        if (request.Latitude.HasValue && request.Longitude.HasValue)
            return ((double) request.Latitude.Value, (double) request.Longitude.Value);

        try
        {
            var zipInfo = addressParser.GetInfoForZipCode(request.PostalCode);
            if (zipInfo?.Latitude != null && zipInfo?.Longitude != null)
            {
                return ((double)zipInfo.Latitude.Value, (double)zipInfo.Longitude.Value);
            }
        }
        catch
        {
            // Swallow and fall through
        }
        return (null, null);
    }

    private void SetDistance(BedRequest request, BedRequest target, double targetLatitude, double targetLongitude, AddressParser addressParser)
    {
        const double defaultDistance = 999;
        request.Distance = defaultDistance;

        if (request.BedRequestId == target.BedRequestId)
        {
            request.Distance = -1;
            return;
        }

        if (request.Status != BedRequestStatus.Waiting)
            return;

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            request.Distance = CalculateDistance(targetLatitude, targetLongitude, (double)request.Latitude.Value, (double)request.Longitude.Value);
        }
        else if (Validation.IsValidZipCode(request.PostalCode))
        {
            try
            {
                request.Distance = addressParser.GetDistanceInMilesBetweenTwoZipCodes(target.PostalCode, request.PostalCode);
            }
            catch
            {
                // Leave default distance
            }
        }
    }

    private async Task<List<BedRequest>> SortScheduledBedRequests(int locationId, List<BedRequest> bedRequests)
    {
        var locationResult = await _locationDataService.GetByIdAsync(locationId);

        if (!locationResult.Success || locationResult.Data == null || !Validation.IsValidZipCode(locationResult.Data.MailingPostalCode))
        {
            return bedRequests.OrderBy(o => o.Team).ThenBy(o => o.PostalCode).ToList();
        }

        var location = locationResult.Data;
        var addressParser = LibraryFactory.CreateAddressParser();

        foreach (var bedRequest in bedRequests)
        {
            bedRequest.Distance = 0;

            if (location.Latitude.HasValue && location.Longitude.HasValue && bedRequest.Latitude.HasValue && bedRequest.Longitude.HasValue)
            {
                bedRequest.Distance = CalculateDistance((double)location.Latitude.Value, (double)location.Longitude.Value,
                    (double)bedRequest.Latitude.Value, (double)bedRequest.Longitude.Value);
            }
            else if (location.MailingPostalCode != bedRequest.PostalCode && Validation.IsValidZipCode(bedRequest.PostalCode))
            {
                try
                {
                    bedRequest.Distance = addressParser.GetDistanceInMilesBetweenTwoZipCodes(location.MailingPostalCode, bedRequest.PostalCode);
                }
                catch (Exception)
                {
                    // Can't determine distance, default to distance of zero 
                }
            }
        }

        return bedRequests.OrderBy(o => o.Team).ThenBy(o => o.Distance).ThenBy(o => o.CreateDate).ToList();
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 3956; // miles
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double d = R * c;
        return d;
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctPhone()
    {
        return await _commonService.GetDistinctPhone(this);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctPhoneByLocation(int locationId)
    {
        return await _commonService.GetDistinctPhoneByLocation(this, locationId);
    }

    public async Task<ServiceResponse<List<string>>> PhonesForNotReceivedABed(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"PhonesForNotReceivedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for PhonesForNotReceivedABed", true, cachedContent); ;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && !string.IsNullOrEmpty(o.Phone)
                                                && o.Status == BedRequestStatus.Waiting).Select(b => b.Phone).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> PhonesForReceivedABed(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"PhonesForReceivedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for PhonesForReceivedABed", true, cachedContent); ;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && !string.IsNullOrEmpty(o.Phone)
                                                && (o.Status == BedRequestStatus.Delivered || o.Status == BedRequestStatus.Given)).Select(b => b.Phone.FormatPhoneNumber()).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> PhonesForSchedule(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"PhonesForSchedule({locationId})");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for PhonesForSchedule", true, cachedContent); ;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId 
                                                && !string.IsNullOrEmpty(o.Phone)
                                                && o.Status == BedRequestStatus.Scheduled).Select(b => b.Phone.FormatPhoneNumber()).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
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

    private string GetWaitTimeCacheKey(int locationId)
        => _cachingService.BuildCacheKey(GetEntityName(), $"GetEstimatedWaitTime({locationId})");

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
            if (row.Month >= 1 && row.Month <= 12) delPrevYear[row.Month - 1] = row.Count;
        }
        foreach (var row in data.Where(r => r.Year == currentYear))
        {
            if (row.Month >= 1 && row.Month <= currentMonth) delCurrentYtd[row.Month - 1] = row.Count;
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

    
    public async Task<ServiceResponse<DateTime?>> NextDateEligibleForBedRequest(NewBedRequest bedRequest)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => (o.Status == BedRequestStatus.Delivered || o.Status == BedRequestStatus.Given)
                && (o.Phone == bedRequest.FormattedPhone || o.Phone == StringUtil.ExtractDigits(bedRequest.Phone) || o.Email == bedRequest.Email))
                    .OrderByDescending(o => o.DeliveryDate)
                    .FirstOrDefaultAsync();

            if (result == null || !result.DeliveryDate.HasValue)
            {
                return new ServiceResponse<DateTime?>("No previous bed request", true, null);
            }

            int monthsBetweenRequests = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                ConfigNames.MonthsBetweenRequests, bedRequest.LocationId);

            if (monthsBetweenRequests <= 0)
            {
                return new ServiceResponse<DateTime?>("No restriction on months between requests", true, null);
            }

            DateTime nextEligibleDate = result.DeliveryDate.Value.AddMonths(monthsBetweenRequests).AddDays(1);
            return new ServiceResponse<DateTime?>("Next eligible date.", true, nextEligibleDate);
        }
    }

    public async Task<ServiceResponse<List<BedRequestDashboardRow>>> GetWaitingDashboard(int userLocationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetWaitingDashboard({userLocationId})");
        var cachedContent = _cachingService.Get<List<BedRequestDashboardRow>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<BedRequestDashboardRow>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for GetWaitingDashboard", true, cachedContent);

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

            // Order: user's location first, then alphabetical
            var ordered = grouped
                .OrderByDescending(r => r.LocationId == userLocationId)
                .ThenBy(r => r.LocationName)
                .ToList();

            // Add totals row
            ordered.Add(new BedRequestDashboardRow
            {
                LocationName = "Total",
                Requests = ordered.Sum(r => r.Requests),
                Beds = ordered.Sum(r => r.Beds)
            });

            _cachingService.Set(cacheKey, ordered);
            return new ServiceResponse<List<BedRequestDashboardRow>>($"Found {ordered.Count} dashboard rows", true, ordered);
        }
    }

    public async Task<ServiceResponse<List<BedRequestHistoryRow>>> GetBedRequestHistory(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetBedRequestHistory({locationId})");
        var cachedContent = _cachingService.Get<List<BedRequestHistoryRow>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<BedRequestHistoryRow>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for GetBedRequestHistory", true, cachedContent);
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
                        Count = g.Sum(x => x.NumberOfBeds)
                    })
                    .ToListAsync();
            }
            else
            {
               result = await dbSet
                    .Where(o => o.LocationId == locationId && o.CreateDate.HasValue && years.Contains(o.CreateDate.Value.Year) && o.CreateDate.Value <= now)
                    .GroupBy(o => new { Year = o.CreateDate.Value.Year, Month = o.CreateDate.Value.Month })
                    .Select(g => new BedRequestHistoryRow
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Sum(x => x.NumberOfBeds)
                    })
                    .ToListAsync();
            }

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<BedRequestHistoryRow>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

   
    public async Task<ServiceResponse<List<BedRequestHistoryRow>>> GetBedDeliveryHistory(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetBedDeliveryHistory({locationId})");
        var cachedContent = _cachingService.Get<List<BedRequestHistoryRow>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<BedRequestHistoryRow>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for GetBedDeliveryHistory", true, cachedContent);
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
                    Count = g.Sum(x => x.NumberOfBeds)
                })
                .ToListAsync();

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<BedRequestHistoryRow>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    private static IQueryable<BedRequest> BuildDeliveryHistoryQuery(IQueryable<BedRequest> source, int locationId, DateTime now)
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
}


