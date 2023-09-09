
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class VolunteerEventsDataService : Repository<VolunteerEvent>, IVolunteerEventsDataService
{
    private readonly ICommonService _commonService;

    public VolunteerEventsDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider, ICommonService commonService) : base(contextFactory, cachingService, authProvider)
    {
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<VolunteerEvent>>> GetAllForLocationAsync()
    {
        return await _commonService.GetAllForLocationAsync(this);
    }

}



