using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using KellermanSoftware.AddressParser;
using Microsoft.EntityFrameworkCore;
using Serilog;

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
    private readonly AddressParser _addressParser;
    
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
        _addressParser = LibraryFactory.CreateAddressParser();
    }

    public override async Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest entity)
    {
        var result = await base.CreateAsync(entity);
        _cachingService.ClearScheduleRelated();

        var tasks = new List<Task>();

        tasks.Add(QueueForGeoLocation(entity));

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

    public override async Task<ServiceResponse<BedRequest>> UpdateAsync(BedRequest entity)
    {
        var allLocationsResponse = await _locationDataService.GetAllAsync();
        if (!allLocationsResponse.Success || allLocationsResponse.Data == null)
        {
            return new ServiceResponse<BedRequest>("Unable to retrieve locations for update", false, null);
        }

        //Force Get
        _cachingService.ClearByEntityName(GetEntityName());
        var previousBedRequest = await GetByIdAsync(entity.BedRequestId);

        if (!previousBedRequest.Success || previousBedRequest.Data == null)
        {
            return new ServiceResponse<BedRequest>($"BedRequest with BedRequestId {entity.BedRequestId} not found",
                false, null);
        }

        bool geoLocationUpdateNeeded = !entity.Latitude.HasValue
                                       || !entity.Longitude.HasValue
                                       || previousBedRequest.Data.Street != entity.Street
                                       || previousBedRequest.Data.City != entity.City
                                       || previousBedRequest.Data.State != entity.State
                                       || previousBedRequest.Data.PostalCode != entity.PostalCode;

        //The user changed the group but not the associated location
        if (previousBedRequest.Data.LocationId == entity.LocationId
            && previousBedRequest.Data.Group != entity.Group
            && allLocationsResponse.Data.Any(o => o.Group?.ToLower() == entity.Group?.ToLower()))
        {
            entity.LocationId = allLocationsResponse.Data.First(o => o.Group == entity.Group).LocationId;
        }
        //The user changed the location but not the associated group
        else if (previousBedRequest.Data.LocationId != entity.LocationId
                 && previousBedRequest.Data.Group == entity.Group)
        {
            entity.Group = allLocationsResponse.Data.First(o => o.LocationId == entity.LocationId).Group;
        }

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

    public async Task<ServiceResponse<List<BedRequest>>> GetBedRequestsForUser()
    {
        ServiceResponse<List<int>> locationsResponse = await _locationDataService.GetValidLocationIdsForUser();

        if (!locationsResponse.Success || locationsResponse.Data == null)
        {
            return new ServiceResponse<List<BedRequest>>(locationsResponse.Message);
        }

        return await GetAllForLocationList(locationsResponse.Data);
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
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), "EmailsForNotReceivedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>(
                $"Found {cachedContent.Count} {GetEntityName()} records in cache for EmailsForNotReceivedABed", true,
                cachedContent);
        ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && !string.IsNullOrEmpty(o.Email)
                                                && o.Status == BedRequestStatus.Waiting).Select(b => b.Email).Distinct()
                .ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> EmailsForReceivedABed(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), "RecievedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>(
                $"Found {cachedContent.Count} {GetEntityName()} records in cache for EmailsForReceivedABed", true,
                cachedContent);
        ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId && !string.IsNullOrEmpty(o.Email)
                                                                           && (o.Status == BedRequestStatus.Delivered ||
                                                                               o.Status == BedRequestStatus.Given))
                .Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> EmailsForSchedule(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"EmailsForSchedule({locationId})");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<string>>(
                $"Found {cachedContent.Count} {GetEntityName()} records in cache for EmailsForSchedule", true,
                cachedContent);
        ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && !string.IsNullOrEmpty(o.Email)
                                                && o.Status == BedRequestStatus.Scheduled).Select(b => b.Email)
                .Distinct().ToListAsync();
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
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"ScheduledBedRequestsForLocation({locationId})");
        var cachedContent = _cachingService.Get<List<BedRequest>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<BedRequest>>(
                $"Found {cachedContent.Count} {GetEntityName()} records in cache for GetScheduledBedRequestsForLocation",
                true, cachedContent);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && o.Status == BedRequestStatus.Scheduled).ToListAsync();

            result = await SortScheduledBedRequests(locationId, result);
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<BedRequest>>($"Found {result.Count} {GetEntityName()} records", true,
                result);
        }
    }

    public List<BedRequest> SortBedRequestClosestToAddress(List<BedRequest> bedRequests, int bedRequestId)
    {
        try
        {
            var targetBedRequest = bedRequests.FirstOrDefault(b => b.BedRequestId == bedRequestId);
            PerfLog.Log("SortClosest FirstOrDefault completed");
            if (targetBedRequest == null)
            {
                return bedRequests;
            }

            targetBedRequest.Distance = -1;
            PerfLog.Log("SortClosest AddressParser created");
            
            var waitingRequests = bedRequests
                .Where(b => b.BedRequestId != targetBedRequest.BedRequestId && b.Status == BedRequestStatus.Waiting)
                .ToList();

            PerfLog.Log("SortClosest WaitingRequests filtered");
            
            var routeOrderedWaitingRequests = OrderByBestRoute(
                waitingRequests,
                targetBedRequest.Latitude.HasValue ? (double?)targetBedRequest.Latitude.Value : null,
                targetBedRequest.Longitude.HasValue ? (double?)targetBedRequest.Longitude.Value : null,
                targetBedRequest.PostalCode);

            PerfLog.Log("SortClosest OrderByBestRoute completed");
            var result = new List<BedRequest> { targetBedRequest };
            result.AddRange(routeOrderedWaitingRequests);
            PerfLog.Log("SortClosest AddRange completed");
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while sorting bed requests closest to address");
            return bedRequests;
        }
    }

    public async Task<ServiceResponse<BedRequest>> GetWaitingByEmail(string email)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var bedRequest =
                await dbSet.FirstOrDefaultAsync(o => o.Email == email && o.Status == BedRequestStatus.Waiting);
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
            var bedRequest = await dbSet.FirstOrDefaultAsync(o =>
                o.Status == BedRequestStatus.Waiting && (o.Phone == phoneWithNumbersOnly || o.Phone == formattedPhone));
            if (bedRequest == null)
            {
                return new ServiceResponse<BedRequest>($"No waiting BedRequest found for phone {phone}", false, null);
            }

            return new ServiceResponse<BedRequest>($"Found waiting BedRequest for phone {phone}", true, bedRequest);
        }
    }

    public async Task<ServiceResponse<DateTime?>> NextDateEligibleForBedRequest(NewBedRequest bedRequest)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o =>
                    (o.Status == BedRequestStatus.Delivered || o.Status == BedRequestStatus.Given)
                    && (o.Phone == bedRequest.FormattedPhone || o.Phone == StringUtil.ExtractDigits(bedRequest.Phone) ||
                        o.Email == bedRequest.Email))
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

    public async Task<List<BedRequest>> SortScheduledBedRequests(int locationId, List<BedRequest> bedRequests)
    {
        try
        {
            var locationResult = await _locationDataService.GetByIdAsync(locationId);

            if (!locationResult.Success || locationResult.Data == null ||
                !Validation.IsValidZipCode(locationResult.Data.BuildPostalCode))
            {
                return bedRequests.OrderBy(o => o.Team).ThenBy(o => o.PostalCode).ToList();
            }

            var location = locationResult.Data;
            var addressParser = LibraryFactory.CreateAddressParser();
            var sortedRequests = new List<BedRequest>();

            var groupedByTeam = bedRequests
                .GroupBy(o => o.Team)
                .OrderBy(o => o.Key)
                .ToList();

            foreach (var teamGroup in groupedByTeam)
            {
                var routeOrdered = OrderByBestRoute(
                    teamGroup.ToList(),
                    location.Latitude.HasValue ? (double?)location.Latitude.Value : null,
                    location.Longitude.HasValue ? (double?)location.Longitude.Value : null,
                    location.BuildPostalCode);

                sortedRequests.AddRange(routeOrdered);
            }

            return sortedRequests;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sorting scheduled bed requests.");
            return bedRequests;
        }
    }

    private List<BedRequest> OrderByBestRoute(List<BedRequest> bedRequests,
        double? startLatitude,
        double? startLongitude,
        string? startPostalCode)
    {
        ArgumentNullException.ThrowIfNull(bedRequests);

        var ordered = new List<BedRequest>(bedRequests.Count);
        var remaining = new HashSet<BedRequest>(bedRequests);
        var postalCodeDistanceCache = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var currentLatitude = startLatitude;
        var currentLongitude = startLongitude;
        var currentPostalCode = startPostalCode;

        while (remaining.Count > 0)
        {
            BedRequest? nextRequest = null;
            double nextDistance = double.MaxValue;
            DateTime nextCreateDate = DateTime.MaxValue;
            int nextBedRequestId = int.MaxValue;

            foreach (var request in remaining)
            {
                var distance = GetDistanceFromPointToBedRequest(currentLatitude, currentLongitude, currentPostalCode,
                    request, postalCodeDistanceCache);
                var createDate = request.CreateDate ?? DateTime.MaxValue;

                if (IsBetterRouteCandidate(distance, createDate, request.BedRequestId, nextDistance, nextCreateDate,
                        nextBedRequestId))
                {
                    nextRequest = request;
                    nextDistance = distance;
                    nextCreateDate = createDate;
                    nextBedRequestId = request.BedRequestId;
                }
            }

            if (nextRequest == null)
            {
                break;
            }

            nextRequest.Distance = nextDistance;
            ordered.Add(nextRequest);
            remaining.Remove(nextRequest);

            currentLatitude = nextRequest.Latitude.HasValue ? (double?)nextRequest.Latitude.Value : null;
            currentLongitude = nextRequest.Longitude.HasValue ? (double?)nextRequest.Longitude.Value : null;
            currentPostalCode = nextRequest.PostalCode;
        }

        return ordered;
    }

    private static bool IsBetterRouteCandidate(double distance,
        DateTime createDate,
        int bedRequestId,
        double bestDistance,
        DateTime bestCreateDate,
        int bestBedRequestId)
    {
        const double distanceTolerance = 0.000001d;

        var isCloser = distance < bestDistance - distanceTolerance;
        if (isCloser)
        {
            return true;
        }

        var isSameDistance = Math.Abs(distance - bestDistance) <= distanceTolerance;
        if (!isSameDistance)
        {
            return false;
        }

        return createDate < bestCreateDate
               || (createDate == bestCreateDate && bedRequestId < bestBedRequestId);
    }

    private double GetDistanceFromPointToBedRequest(double? startLatitude,
        double? startLongitude,
        string? startPostalCode,
        BedRequest request,
        Dictionary<string, double> postalCodeDistanceCache)
    {
        const double defaultDistance = 999;

        if (startLatitude.HasValue && startLongitude.HasValue && request.Latitude.HasValue && request.Longitude.HasValue)
        {
            return CalculateDistance(startLatitude.Value, startLongitude.Value, (double)request.Latitude.Value,
                (double)request.Longitude.Value);
        }

        if (!Validation.IsValidZipCode(startPostalCode) || !Validation.IsValidZipCode(request.PostalCode))
        {
            return defaultDistance;
        }

        if (startPostalCode == request.PostalCode)
        {
            return 0;
        }

        var normalizedStartPostalCode = startPostalCode!;
        var normalizedDestinationPostalCode = request.PostalCode!;
        var cacheKey = BuildPostalCodeDistanceCacheKey(normalizedStartPostalCode, normalizedDestinationPostalCode);
        if (postalCodeDistanceCache.TryGetValue(cacheKey, out var cachedDistance))
        {
            return cachedDistance;
        }

        try
        {
            var distance = _addressParser.GetDistanceInMilesBetweenTwoZipCodes(normalizedStartPostalCode,
                normalizedDestinationPostalCode);
            postalCodeDistanceCache[cacheKey] = distance;
            return distance;
        }
        catch
        {
            postalCodeDistanceCache[cacheKey] = defaultDistance;
            return defaultDistance;
        }
    }

    private static string BuildPostalCodeDistanceCacheKey(string startPostalCode, string destinationPostalCode)
    {
        return string.Compare(startPostalCode, destinationPostalCode, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{startPostalCode}|{destinationPostalCode}"
            : $"{destinationPostalCode}|{startPostalCode}";
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
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
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), "PhonesForNotReceivedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<string>>(
                $"Found {cachedContent.Count} {GetEntityName()} records in cache for PhonesForNotReceivedABed", true,
                cachedContent);
        ;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && !string.IsNullOrEmpty(o.Phone)
                                                && o.Status == BedRequestStatus.Waiting).Select(b => b.Phone).Distinct()
                .ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> PhonesForReceivedABed(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), "PhonesForReceivedABed");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<string>>(
                $"Found {cachedContent.Count} {GetEntityName()} records in cache for PhonesForReceivedABed", true,
                cachedContent);
        ;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && !string.IsNullOrEmpty(o.Phone)
                                                && (o.Status == BedRequestStatus.Delivered ||
                                                    o.Status == BedRequestStatus.Given))
                .Select(b => b.Phone.FormatPhoneNumber()).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<string>>> PhonesForSchedule(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"PhonesForSchedule({locationId})");
        var cachedContent = _cachingService.Get<List<string>>(cacheKey);
        if (cachedContent != null)
            return new ServiceResponse<List<string>>(
                $"Found {cachedContent.Count} {GetEntityName()} records in cache for PhonesForSchedule", true,
                cachedContent);
        ;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            var result = await dbSet.Where(o => o.LocationId == locationId
                                                && !string.IsNullOrEmpty(o.Phone)
                                                && o.Status == BedRequestStatus.Scheduled)
                .Select(b => b.Phone.FormatPhoneNumber()).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count} {GetEntityName()} records", true, result);
        }
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetBedRequestsByUserAndStatus(List<BedRequestStatus> statuses)
    {
        ServiceResponse<List<BedRequest>> bedRequests = await GetBedRequestsForUser();

        if (!bedRequests.Success || bedRequests.Data == null)
        {
            return new ServiceResponse<List<BedRequest>>(bedRequests.Message);
        }

        var filteredResult = bedRequests.Data
            .Where(br => statuses.Contains(br.Status))
            .ToList();
        return new ServiceResponse<List<BedRequest>>(
            $"Found {filteredResult.Count} bed requests with matching statuses", true, filteredResult);
    }
}






