using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;


namespace BedBrigade.Data.Services;

public class VolunteerForDataService : Repository<VolunteerFor>, IVolunteerForDataService
{
    public VolunteerForDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
    {
    }
}



