using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;

using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class MetroAreaDataService : Repository<MetroArea>, IMetroAreaDataService
    {
        public MetroAreaDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService,
            IAuthService authService,
            ILocationDataService locationDataService) : base(contextFactory, cachingService, authService)
        {
        }
    }
}
