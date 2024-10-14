using BedBrigade.Common.Models;
using BedBrigade.Client.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Data.Common;

namespace BedBrigade.Data.Services
{
    public class TranslationDataService : Repository<Translation>, ITranslationDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;

        public TranslationDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
        }

        public async Task<ServiceResponse<List<Translation>>> GetTranslationsForLanguage(string languageCode)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetTranslationsForLanguage({languageCode})");
            List<Translation>? cachedContent = _cachingService.Get<List<Translation>>(cacheKey);

            if (cachedContent != null)
            {
                return new ServiceResponse<List<Translation>>($"Found {cachedContent.Count()} {GetEntityName()} in cache", true, cachedContent);
            }

            try
            {
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<Translation>();
                    var result = await dbSet.Where(o => o.Culture == languageCode).ToListAsync();
                    _cachingService.Set(cacheKey, result);
                    return new ServiceResponse<List<Translation>>($"Found {result.Count()} {GetEntityName()}", true, result);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<List<Translation>>($"Error GetAllAsync for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
            }
        }
    }
}
