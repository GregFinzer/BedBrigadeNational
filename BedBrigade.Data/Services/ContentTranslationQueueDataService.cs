using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class ContentTranslationQueueDataService : Repository<ContentTranslationQueue>, IContentTranslationQueueDataService
    {
        public ContentTranslationQueueDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, IAuthService authService) : base(contextFactory, cachingService, authService)
        {
        }
    }
}
