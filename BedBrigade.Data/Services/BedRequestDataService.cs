using System.Data.Common;
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

    private void SetContacted(BedRequest entity)
    {
        if (entity.Contacted)
            return;

        string[] words =
        {
            "lm",
            "txt",
            "msg",
            "message",
            "called",
            "spoke",
            "phoned"
        };

        entity.Contacted = entity.Status != BedRequestStatus.Waiting
                           || words.Any(o => (entity.Notes ?? string.Empty).ToLower().Contains(o))
                           || (entity.Reference ?? string.Empty).ToLower().Contains("phone");
    }
    
    public override async Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest entity)
    {
        SetContacted(entity);
        
        //Always set the longitude and latitude if the postal code is valid
        var parser = LibraryFactory.AddressParser;

        if (parser.IsValidZipCode(entity.PostalCode))
        {
            var info = parser.GetInfoForZipCode(entity.PostalCode);
            if (info != null)
            {
                entity.Latitude = info.Latitude;
                entity.Longitude = info.Longitude;
            }
        }

        var result = await base.CreateAsync(entity);
        _cachingService.ClearScheduleRelated();

        var tasks = new List<Task>();

        if (!String.IsNullOrWhiteSpace(entity.Street)
            && !String.IsNullOrWhiteSpace(entity.City)
            && !String.IsNullOrWhiteSpace(entity.State)
            && !String.IsNullOrWhiteSpace(entity.PostalCode))
        {
            tasks.Add(QueueForGeoLocation(entity));
        }

        if (entity.Status == BedRequestStatus.Scheduled)
        {
            var scheduled = await GetScheduledBedRequestsForLocation(entity.LocationId);
            if (scheduled.Success && scheduled.Data != null)
            {
                tasks.Add(_scheduleDataService.UpdateBedRequestSummaryInformation(
                    entity.LocationId, scheduled.Data));
            }
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
        SetContacted(entity);
        
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

        bool geoLocationUpdateNeeded = GeoLocationUpdateNeeded(entity, previousBedRequest.Data);

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

    private static bool GeoLocationUpdateNeeded(BedRequest entity, BedRequest previousBedRequest)
    {
        bool geoLocationUpdateNeeded = (entity.Longitude == null
                                        || entity.Latitude == null
                                        || previousBedRequest.Street != entity.Street
                                        || previousBedRequest.City != entity.City
                                        || previousBedRequest.State != entity.State
                                        || previousBedRequest.PostalCode != entity.PostalCode)
                                       && !String.IsNullOrWhiteSpace(entity.Street)
                                       && !String.IsNullOrWhiteSpace(entity.City)
                                       && !String.IsNullOrWhiteSpace(entity.State)
                                       && !String.IsNullOrWhiteSpace(entity.PostalCode);
        return geoLocationUpdateNeeded;
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
        try
        {
            int bedRequestId = Convert.ToInt32(id);
            await DeleteAssociatedSms(bedRequestId);
            await DeleteAssociatedEmail(bedRequestId);
            var result = await base.DeleteAsync(id);
            _cachingService.ClearScheduleRelated();
            return result;
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"Could not DeleteAsync {GetEntityName()}  with id {id}: {ex.Message} ({ex.ErrorCode})", false);
        }
    }

    private async Task DeleteAssociatedEmail(int bedRequestId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<EmailQueue>();
            var result = await dbSet.Where(o => o.BedRequestId == bedRequestId)
                .ToListAsync();

            dbSet.RemoveRange(result);
            await ctx.SaveChangesAsync();
            _cachingService.ClearByEntityName(nameof(EmailQueue));
        }
    }

    private async Task DeleteAssociatedSms(int bedRequestId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            var result = await dbSet.Where(o => o.BedRequestId == bedRequestId)
                .ToListAsync();

            dbSet.RemoveRange(result);
            await ctx.SaveChangesAsync();
            _cachingService.ClearByEntityName(nameof(SmsQueue));
        }
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetAllForScheduleId(int scheduleId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetAllForScheduleId({scheduleId})");
        var cachedContent = _cachingService.Get<List<BedRequest>>(cacheKey);
        
        if (cachedContent != null)
            return new ServiceResponse<List<BedRequest>>($"Found {cachedContent.Count} {GetEntityName()} records in cache for GetAllForScheduleId", true,
                cachedContent);
        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<BedRequest>();
                var result = await dbSet.Where(o => o.ScheduleId == scheduleId)
                    .ToListAsync();

                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<BedRequest>>("Found for ScheduleId", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<BedRequest>>($"Could not GetAllForScheduleId {GetEntityName()}  with scheduleId {scheduleId}: {ex.Message} ({ex.ErrorCode})", false);
        }
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

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<BedRequest>>($"Found {result.Count} {GetEntityName()} records", true,
                result);
        }
    }






    public async Task<int> MarkInvalidEmailForWaitingForBedRequest(List<string> emailList)
    {
        string userName = GetUserName() ?? Defaults.DefaultUserNameAndEmail;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var lowerEmailList = emailList.Select(e => e.ToLower()).ToList();

            int updated = await ctx.Set<BedRequest>()
                .Where(o => lowerEmailList.Contains(o.Email.ToLower())
                            && o.Status == BedRequestStatus.Waiting
                            && o.Notes != null
                            && !o.Notes.Contains("Invalid Email"))
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(o => o.UpdateUser, userName)
                    .SetProperty(o => o.UpdateDate, DateTime.UtcNow)
                    .SetProperty(o => o.MachineName, Environment.MachineName)
                    .SetProperty(o => o.Notes,
                        o => (o.Notes ?? "") + " | Invalid Email"));

            if (updated > 0)
            {
                _cachingService.ClearScheduleRelated();
            }

            return updated;
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






