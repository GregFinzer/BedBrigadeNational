using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IDonationDataService : IRepository<Donation>
    {
        Task<ServiceResponse<List<Donation>>> GetAllForLocationAsync(int locationId);
        Task<ServiceResponse<List<Donation>>> GetByYearAsync(int year);
    }
}