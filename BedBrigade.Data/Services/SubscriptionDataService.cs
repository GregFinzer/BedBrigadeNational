using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class SubscriptionDataService : Repository<Subscription>, ISubscriptionDataService
    {
        private readonly ICachingService _cachingService;
        private readonly IDbContextFactory<DataContext> _contextFactory;

        public SubscriptionDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
            _cachingService = cachingService;
            _contextFactory = contextFactory;
        }

        public async Task<ServiceResponse<List<Subscription>>> GetSubscriptionsByNewsletterAsync(int newsletterId)
        {
            string cacheKey = _cachingService.BuildCacheKey("GetSubscriptionsByNewsletterAsync", newsletterId);
            var cachedSubscriptions = _cachingService.Get<List<Subscription>>(cacheKey);
            if (cachedSubscriptions != null)
            {
                return new ServiceResponse<List<Subscription>>($"Found {GetEntityName()} in cache", true, cachedSubscriptions);
            }
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Subscription>();
                var result = await dbSet.Where(c => c.NewsletterId == newsletterId).ToListAsync();
                if (result == null)
                {
                    return new ServiceResponse<List<Subscription>>($"Could not find {GetEntityName()} with newsletterId of {newsletterId}", false, null);
                }
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Subscription>>($"Found {GetEntityName()} with newsletterId of {newsletterId}", true, result);
            }
        }
    }
}
