using System.Data.Common;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestEmailDataService : Repository<BedRequest>, IBedRequestEmailDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICommonService _commonService;
    private  readonly ICachingService _cachingService;
    
    public BedRequestEmailDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, IAuthService authService, ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _commonService = commonService;
        _cachingService = cachingService;
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
    
    public async Task<ServiceResponse<int>> SumBedsForNotReceived(int locationId)
    {
        try
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
        catch (DbException ex)
        {
            return new ServiceResponse<int>($"Could not SumBedsForNotReceived {GetEntityName()} with locationId {locationId}: {ex.Message} ({ex.ErrorCode})", false);
        }
    }
}