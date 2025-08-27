using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Data.Common;
using BedBrigade.SpeakIt;

namespace BedBrigade.Data.Services
{
    public class TranslationDataService : Repository<Translation>, ITranslationDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;
        private readonly ITranslateLogic _translateLogic;

        public TranslationDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService, ITranslateLogic translateLogic) : base(contextFactory, cachingService, authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
            _translateLogic = translateLogic;
        }

        //This will attempt to get the translation using the language container and then the list of translations in the database
        public async Task<string> GetTranslation(string? value, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var resourceTranslation = _translateLogic.GetTranslation(value);

            if (resourceTranslation != value)
            {
                return resourceTranslation;
            }

            var translationsForLanguage = await GetTranslationsForLanguage(languageCode);
            if (!translationsForLanguage.Success || translationsForLanguage.Data == null)
            {
                return value;
            }

            var hash = _translateLogic.ComputeSHA512Hash(value);
            var translation = translationsForLanguage.Data.FirstOrDefault(t => t.Hash == hash);

            if (translation == null)
            {
                return value;
            }

            return translation.Content;
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
