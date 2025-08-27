using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IDonationCampaignDataService : IRepository<DonationCampaign>
    {
        Task<ServiceResponse<List<DonationCampaign>>> GetAllForLocationAsync(int locationId);
    }
}
