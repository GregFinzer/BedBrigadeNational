using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BedBrigade.Data.Services
{
    public class ContentTranslationQueueDataService : Repository<ContentTranslationQueue>, IContentTranslationQueueDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;

        public ContentTranslationQueueDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
        }

        public async Task<ServiceResponse<string>> QueueContentTranslation(ContentTranslationQueue contentTranslation)
        {
            try
            {
                contentTranslation.Status = QueueStatus.Queued.ToString();
                contentTranslation.QueueDate = DateTime.UtcNow;
                contentTranslation.FailureMessage = string.Empty;
                await CreateAsync(contentTranslation);
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>(ex.Message, false);
            }

            return new ServiceResponse<string>(QueueStatus.Queued.ToString(), true);
        }


        public async Task<List<ContentTranslationQueue>> GetContentTranslationsToProcess(int maxPerChunk)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetContentTranslationsToProcess()");
            List<ContentTranslationQueue>? cachedContent = _cachingService.Get<List<ContentTranslationQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<ContentTranslationQueue>();
                var result = await dbSet.Where(o => o.Status == QueueStatus.Queued.ToString())
                    .OrderBy(o => o.QueueDate)
                    .Take(maxPerChunk)
                    .ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task<List<ContentTranslationQueue>> GetLockedContentTranslations()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetLockedContentTranslations()");
            List<ContentTranslationQueue>? cachedContent = _cachingService.Get<List<ContentTranslationQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<ContentTranslationQueue>();
                var result = await dbSet.Where(o => o.Status == QueueStatus.Locked.ToString()).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task LockContentTranslationsToProcess(List<ContentTranslationQueue> contentTranslationsToProcess)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<ContentTranslationQueue>();
                foreach (var item in contentTranslationsToProcess)
                {
                    item.LockDate = DateTime.UtcNow;
                    item.Status = QueueStatus.Locked.ToString();
                    item.UpdateDate = DateTime.UtcNow;
                    dbSet.Update(item);
                }
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }

        public async Task ClearContentTranslationQueueLock()
        {
            var lockedTranslations = await GetLockedContentTranslations();

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<ContentTranslationQueue>();
                foreach (var lockedTranslation in lockedTranslations)
                {
                    lockedTranslation.LockDate = null;
                    lockedTranslation.Status = QueueStatus.Queued.ToString();
                    lockedTranslation.UpdateDate = DateTime.UtcNow;
                    dbSet.Update(lockedTranslation);
                }
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }

        public async Task DeleteOldContentTranslationQueue(int daysOld)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<ContentTranslationQueue>();
                var oldTranslationQueue = dbSet.Where(o =>
                    o.Status != QueueStatus.Queued.ToString() && o.UpdateDate < DateTime.UtcNow.AddDays(-daysOld));
                dbSet.RemoveRange(oldTranslationQueue);
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }
    }
}
