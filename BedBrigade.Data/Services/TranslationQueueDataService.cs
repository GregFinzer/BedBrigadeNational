using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Services
{
    public class TranslationQueueDataService : Repository<TranslationQueue>, ITranslationQueueDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;

        public TranslationQueueDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
        }

        public async Task<ServiceResponse<string>> QueueTranslation(TranslationQueue translation)
        {
            try
            {
                translation.Status = QueueStatus.Queued.ToString();
                translation.QueueDate = DateTime.UtcNow;
                translation.FailureMessage = string.Empty;
                await CreateAsync(translation);
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>(ex.Message, false);
            }

            return new ServiceResponse<string>(QueueStatus.Queued.ToString(), true);
        }


        public async Task<List<TranslationQueue>> GetTranslationsToProcess(int maxPerChunk)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetTranslationsToProcess()");
            List<TranslationQueue>? cachedContent = _cachingService.Get<List<TranslationQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TranslationQueue>();
                var result = await dbSet.Where(o => o.Status == QueueStatus.Queued.ToString())
                    .OrderBy(o => o.QueueDate)
                    .Take(maxPerChunk)
                    .ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task<List<TranslationQueue>> GetLockedTranslations()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetLockedTranslations()");
            List<TranslationQueue>? cachedContent = _cachingService.Get<List<TranslationQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TranslationQueue>();
                var result = await dbSet.Where(o => o.Status == QueueStatus.Locked.ToString()).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task LockTranslationsToProcess(List<TranslationQueue> translationsToProcess)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TranslationQueue>();
                foreach (var item in translationsToProcess)
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

        public async Task ClearTranslationQueueLock()
        {
            var lockedTranslations = await GetLockedTranslations();

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TranslationQueue>();
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

        public async Task DeleteOldTranslationQueue(int daysOld)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TranslationQueue>();
                var oldTranslationQueue = dbSet.Where(o =>
                    o.Status != QueueStatus.Queued.ToString() && o.UpdateDate < DateTime.UtcNow.AddDays(-daysOld));
                dbSet.RemoveRange(oldTranslationQueue);
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }
    }
}
