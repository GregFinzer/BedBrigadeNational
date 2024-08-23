using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IContactUsDataService : IRepository<ContactUs>
    {
        Task<ServiceResponse<List<ContactUs>>> GetAllForLocationAsync();
        Task<ServiceResponse<List<string>>> GetDistinctEmail();
        Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId);
    }
}
