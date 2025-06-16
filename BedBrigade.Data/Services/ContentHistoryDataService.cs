using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class ContentHistoryDataService : Repository<ContentHistory>, IContentHistoryDataService
{
    public ContentHistoryDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService) : base(contextFactory, cachingService, authService)
    {
    }
}


