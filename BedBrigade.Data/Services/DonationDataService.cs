using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class DonationDataService : Repository<Donation>, IDonationDataService
{
    private readonly ICommonService _commonService;

    public DonationDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService, 
        ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<Donation>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }
}



