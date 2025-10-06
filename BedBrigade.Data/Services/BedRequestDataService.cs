using AngleSharp.Dom;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using KellermanSoftware.AddressParser;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestDataService : Repository<BedRequest>, IBedRequestDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;
    private readonly ILocationDataService _locationDataService;
    private readonly IGeoLocationQueueDataService _geoLocationQueueDataService;
    private readonly ITimezoneDataService _timezoneDataService;

    public BedRequestDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService,
        ICommonService commonService,
        ILocationDataService locationDataService,
        IGeoLocationQueueDataService geoLocationQueueDataService, ITimezoneDataService timezoneDataService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
        _locationDataService = locationDataService;
        _geoLocationQueueDataService = geoLocationQueueDataService;
        _timezoneDataService = timezoneDataService;
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

        if (geoLocationUpdateNeeded)
        {
            await QueueForGeoLocation(entity);
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
            var result = await dbSet.Where(o => o.LocationId == locationId && !string.IsNullOrEmpty(o.Email)
                                                && (o.Status == BedRequestStatus.Delivered || o.Status == BedRequestStatus.Given)).Select(b => b.Email).Distinct().ToListAsync();
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
            var result = await dbSet.Where(o => o.LocationId == locationId 
                                                && !string.IsNullOrEmpty(o.Email)
                                                && o.Status == BedRequestStatus.Scheduled).Select(b => b.Email).Distinct().ToListAsync();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
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
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
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
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
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
            return new ServiceResponse<List<string>>($"Found {result.Count()} {GetEntityName()} records", true, result);
        }
    }
}



