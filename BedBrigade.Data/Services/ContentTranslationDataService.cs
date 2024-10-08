using BedBrigade.Common.Models;
using BedBrigade.Client.Services;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class ContentTranslationDataService : Repository<ContentTranslation>, IContentTranslationDataService
    {
        public ContentTranslationDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
        }
    }
}
