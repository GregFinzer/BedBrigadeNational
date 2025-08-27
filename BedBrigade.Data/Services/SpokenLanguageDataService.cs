using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class SpokenLanguageDataService : Repository<SpokenLanguage>, ISpokenLanguageDataService
    {
        public SpokenLanguageDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
        }
    }
}
