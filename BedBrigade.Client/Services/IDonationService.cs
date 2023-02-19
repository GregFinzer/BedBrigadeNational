using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IDonationService
    {
        Task<ServiceResponse<Donation>> CreateAsync(Donation donation);
        Task<ServiceResponse<bool>> DeleteAsync(int donationId);
        Task<ServiceResponse<List<Donation>>> GetAllAsync();
        Task<ServiceResponse<Donation>> UpdateAsync(Donation donation);
    }
}