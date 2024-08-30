using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class MetroAreaDataService : Repository<MetroArea>, IMetroAreaDataService
    {
        public MetroAreaDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            AuthenticationStateProvider authProvider,
            ILocationDataService locationDataService) : base(contextFactory, cachingService, authProvider)
        {
        }
    }
}
