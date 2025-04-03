using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class NewsletterDataService : Repository<Newsletter>, INewsletterDataService
    {
        private readonly ICachingService _cachingService;
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICommonService _commonService;

        public NewsletterDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService,
            ICommonService commonService) : base(contextFactory, cachingService, authService)
        {
            _cachingService = cachingService;
            _contextFactory = contextFactory;
            _commonService = commonService;
        }

        public async Task<ServiceResponse<Newsletter>> GetAsync(string name, int locationId)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), locationId, name);
            var cachedNewsletter = _cachingService.Get<Newsletter>(cacheKey);
            if (cachedNewsletter != null)
            {
                return new ServiceResponse<Newsletter>($"Found {GetEntityName()} in cache", true, cachedNewsletter);
            }
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<Newsletter>();
                var result = await dbSet.Where(c => c.Name == name && c.LocationId == locationId).FirstOrDefaultAsync();
                if (result == null)
                {
                    return new ServiceResponse<Newsletter>($"Could not find {GetEntityName()} with locationId of {locationId} and a name of {name}", false, null);
                }
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<Newsletter>($"Found {GetEntityName()} with locationId of {locationId} and a name of {name}", true, result);
            }
        }

        public async Task<ServiceResponse<List<Newsletter>>> GetAllForLocationAsync(int locationId)
        {
            var contentResult = await _commonService.GetAllForLocationAsync(this, locationId);
            return contentResult;
        }

    }
}
