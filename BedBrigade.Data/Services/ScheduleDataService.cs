using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class ScheduleDataService : Repository<Schedule>, IScheduleDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;

    public ScheduleDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
    }


    public async Task<ServiceResponse<List<Schedule>>> GetFutureSchedulesByLocationId(int locationId)
    {
        string cacheKey =
            _cachingService.BuildCacheKey(GetEntityName(), $"GetFutureSchedulesByLocationId({locationId})");
        List<Schedule>? cachedContent = _cachingService.Get<List<Schedule>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Schedule>>($"Found {cachedContent.Count()} {GetEntityName()} in cache",
                true, cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Schedule>();
                var result = await dbSet
                    .Where(o => o.LocationId == locationId && o.EventDateScheduled.Date >= DateTime.UtcNow.Date)
                    .OrderBy(o => o.EventDateScheduled).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Schedule>>($"Found {result.Count()} {GetEntityName()}", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<Schedule>>(
                $"Error GetFutureSchedulesByLocationId for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
        }
    }
}



