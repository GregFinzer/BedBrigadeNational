using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class SubscriptionDataService : Repository<Subscription>, ISubscriptionDataService
    {
        private readonly ICachingService _cachingService;
        private readonly INewsletterDataService _newsletterDataService;
        private readonly IDbContextFactory<DataContext> _contextFactory;

        public SubscriptionDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService,
            INewsletterDataService newsletterDataService) : base(contextFactory, cachingService, authService)
        {
            _cachingService = cachingService;
            _newsletterDataService = newsletterDataService;
            _contextFactory = contextFactory;
        }

        public async Task<ServiceResponse<bool>> Subscribe(int locationId, string newsletterName, string email)
        {
            var newsletterResponse = await _newsletterDataService.GetAsync(newsletterName, locationId);
            if (!newsletterResponse.Success || newsletterResponse.Data == null)
            {
                return new ServiceResponse<bool>($"Could not find newsletter with name {newsletterName}", false, false);
            }

            //Check to see if we are already subscribed
            var existingSubscriptions = await GetSubscriptionsByNewsletterAsync(newsletterResponse.Data.NewsletterId);

            if (existingSubscriptions.Success && existingSubscriptions.Data != null)
            {
                var existingSubscription = existingSubscriptions.Data.FirstOrDefault(s => s.Email == email);
                if (existingSubscription != null)
                {
                    if (!existingSubscription.Subscribed)
                    {
                        existingSubscription.Subscribed = true;
                        await UpdateAsync(existingSubscription);
                        return new ServiceResponse<bool>($"Re-subscribed to {newsletterName}", true, true);
                    }

                    return new ServiceResponse<bool>($"Already subscribed to {newsletterName}", true, true);
                }
            }

            //Add a new subscription
            var newsletter = newsletterResponse.Data;
            var subscription = new Subscription
            {
                NewsletterId = newsletter.NewsletterId,
                Email = email,
                Subscribed = true
            };

            await CreateAsync(subscription);
            return new ServiceResponse<bool>($"Successfully subscribed to {newsletterName}", true, true);
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
