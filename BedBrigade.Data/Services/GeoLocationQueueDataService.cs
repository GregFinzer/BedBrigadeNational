using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class GeoLocationQueueDataService : Repository<GeoLocationQueue>, IGeoLocationQueueDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;

        public GeoLocationQueueDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
        }

        /// <summary>
        /// Get the count of GeoLocationQueue that has been processed or failed for today
        /// </summary>
        /// <returns></returns>
        public async Task<List<GeoLocationQueue>> GetGeoLocationsToday()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetGeoLocationsToday()");
            List<GeoLocationQueue>? cached = _cachingService.Get<List<GeoLocationQueue>>(cacheKey);

            if (cached != null)
            {
                return cached;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<GeoLocationQueue>();
                var result = await dbSet.Where(o => o.ProcessedDate.HasValue && o.ProcessedDate.Value.Date == DateTime.UtcNow.Date).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task<List<GeoLocationQueue>> GetLockedGeoLocationQueue()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetLockedGeoLocationQueue()");
            List<GeoLocationQueue>? cachedContent = _cachingService.Get<List<GeoLocationQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<GeoLocationQueue>();
                var result = await dbSet.Where(o => o.Status == GeoLocationStatus.Locked.ToString()).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task ClearGeoLocationQueueLock()
        {
            var lockedGeoLocationQueues = await GetLockedGeoLocationQueue();

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<GeoLocationQueue>();
                foreach (var lockedGeoLocationQueue in lockedGeoLocationQueues)
                {
                    lockedGeoLocationQueue.LockDate = null;
                    lockedGeoLocationQueue.Status = GeoLocationStatus.Queued.ToString();
                    lockedGeoLocationQueue.UpdateDate = DateTime.UtcNow;
                    dbSet.Update(lockedGeoLocationQueue);
                }
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }

        public async Task<List<GeoLocationQueue>> GetItemsToProcess()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetItemsToProcess()");
            List<GeoLocationQueue>? cachedContent = _cachingService.Get<List<GeoLocationQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<GeoLocationQueue>();
                var result = await dbSet.Where(o => o.Status == GeoLocationStatus.Queued.ToString())
                    .OrderByDescending(o => o.Priority)
                    .ThenBy(o => o.QueueDate)
                    .ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task LockItemsToProcess(List<GeoLocationQueue> itemsToProcess)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<GeoLocationQueue>();
                foreach (var item in itemsToProcess)
                {
                    item.LockDate = DateTime.UtcNow;
                    item.Status = EmailQueueStatus.Locked.ToString();
                    item.UpdateDate = DateTime.UtcNow;
                    dbSet.Update(item);
                }
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }

        public async Task DeleteOldGeoLocationQueue(int daysOld)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<GeoLocationQueue>();
                var oldItems = dbSet.Where(o =>
                    o.Status != GeoLocationStatus.Queued.ToString() && o.UpdateDate < DateTime.UtcNow.AddDays(-daysOld));
                dbSet.RemoveRange(oldItems);
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }

        public override async Task<ServiceResponse<GeoLocationQueue>> CreateAsync(GeoLocationQueue entity)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var existingRecord = ctx.GeoLocationQueue.Where(o => o.TableId == entity.TableId
                                                                     && o.TableName == entity.TableName
                                                                     && o.Street == entity.Street
                                                                     && o.PostalCode == entity.PostalCode).FirstOrDefault();
                if (existingRecord == null)
                {
                    return await base.CreateAsync(entity);
                }

                return new ServiceResponse<GeoLocationQueue>("Existing queued record found, not adding duplicate", true, existingRecord);
            }
        }
    }
}
