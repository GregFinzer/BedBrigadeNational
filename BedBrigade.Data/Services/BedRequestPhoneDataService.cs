using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestPhoneDataService : Repository<BedRequest>, IBedRequestPhoneDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICommonService _commonService;
    private  readonly ICachingService _cachingService;
    
    public BedRequestPhoneDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, IAuthService authService, ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _commonService = commonService;
        _cachingService = cachingService;
    }

    public async Task<ServiceResponse<BedRequest>> GetByPhone(string phone)
    {
        return await _commonService.GetByPhone(this, phone);
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
}