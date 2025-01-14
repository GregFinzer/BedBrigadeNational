using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class MediaDataService :  Repository<Media>, IMediaDataService
{
    public MediaDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, IAuthService authService) : base(contextFactory, cachingService, authService)
    {
    }
} 



