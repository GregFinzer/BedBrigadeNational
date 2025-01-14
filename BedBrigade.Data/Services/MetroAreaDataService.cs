using BedBrigade.Common.Models;

using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class MetroAreaDataService : Repository<MetroArea>, IMetroAreaDataService
    {
        public MetroAreaDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService,
            IAuthService authService) : base(contextFactory, cachingService, authService)
        {
        }
    }
}
