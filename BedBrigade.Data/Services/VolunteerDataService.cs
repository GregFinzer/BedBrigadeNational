
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class VolunteerDataService : Repository<Volunteer>, IVolunteerDataService
{
    private readonly ICommonService _commonService;

    public VolunteerDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider, ICommonService commonService) : base(contextFactory, cachingService, authProvider)
    {
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<Volunteer>>> GetAllForLocationAsync()
    {
        return await _commonService.GetAllForLocationAsync(this);
    }

}



