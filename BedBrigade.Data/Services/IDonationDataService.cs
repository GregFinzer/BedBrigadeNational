using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IDonationDataService : IRepository<Donation>
    {
        Task<ServiceResponse<List<Donation>>> GetAllForLocationAsync();
    }
}