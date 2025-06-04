using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class ContentTranslationDataService : Repository<ContentTranslation>, IContentTranslationDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;

        public ContentTranslationDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
        }

        public Task<ServiceResponse<ContentTranslation>> GetAsync(string name, int locationId, string culture)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), locationId, name, culture);
            var cachedContent = _cachingService.Get<ContentTranslation>(cacheKey);

            if (cachedContent != null)
            {
                return Task.FromResult(new ServiceResponse<ContentTranslation>($"Found {GetEntityName()} in cache", true, cachedContent));
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<ContentTranslation>();
                var result = dbSet.Where(c => c.Name == name && c.LocationId == locationId && c.Culture == culture).FirstOrDefault();

                if (result == null)
                {
                    return Task.FromResult(new ServiceResponse<ContentTranslation>($"Could not find {GetEntityName()} with locationId of {locationId} and a name of {name} with a culture of {culture}", false, null));
                }

                _cachingService.Set(cacheKey, result);
                return Task.FromResult(new ServiceResponse<ContentTranslation>($"Found {GetEntityName()} with locationId of {locationId} and a name of {name} with a culture of {culture}", true, result));
            }
        }
    }
}
