using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class DonationCampaignDataService : Repository<DonationCampaign>, IDonationCampaignDataService
    {
        private readonly ICommonService _commonService;

        public DonationCampaignDataService(IDbContextFactory<DataContext> contextFactory,
            ICachingService cachingService,
            IAuthService authService,
            ICommonService commonService) : base(contextFactory, cachingService, authService)
        {
            _commonService = commonService;
        }

        public async Task<ServiceResponse<List<DonationCampaign>>> GetAllForLocationAsync(int locationId)
        {
            return await _commonService.GetAllForLocationAsync(this, locationId);
        }
    }
}
