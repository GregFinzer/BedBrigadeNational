using BedBrigade.Shared;

namespace BedBrigade.Server.Services
{
    public interface IDonationService
    {
        Task<ServiceResponse<Donation>> CreateAsync(Donation donation);
        Task<ServiceResponse<bool>> DeleteAsync(string UserName);
        Task<ServiceResponse<List<Donation>>> GetAllAsync();
        Task<ServiceResponse<Donation>> GetAsync(int donationId);
        Task<ServiceResponse<Donation>> UpdateAsync(Donation donation);
    }
}