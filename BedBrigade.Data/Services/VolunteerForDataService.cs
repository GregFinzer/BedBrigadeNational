using BedBrigade.Client.Services;
using BedBrigade.Common.Models;

using Microsoft.EntityFrameworkCore;


namespace BedBrigade.Data.Services;

public class VolunteerForDataService : Repository<VolunteerFor>, IVolunteerForDataService
{
    public VolunteerForDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService) : base(contextFactory, cachingService, authService)
    {
    }
}



