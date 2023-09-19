using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class BedRequestDataService : Repository<BedRequest>, IBedRequestDataService
{
    private readonly ICommonService _commonService;

    public BedRequestDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider,
        ICommonService commonService) : base(contextFactory, cachingService, authProvider)
    {
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetAllForLocationAsync()
    {
        return await _commonService.GetAllForLocationAsync(this);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmail()
    {
        return await _commonService.GetDistinctEmail(this);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId)
    {
        return await _commonService.GetDistinctEmailByLocation(this, locationId);
    }
}



