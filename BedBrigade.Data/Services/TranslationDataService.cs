using BedBrigade.Common.Models;
using BedBrigade.Client.Services;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class TranslationDataService : Repository<Translation>, ITranslationDataService
    {
        public TranslationDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
        }
    }
}
