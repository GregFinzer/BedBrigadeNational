using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IContactUsDataService : IRepository<ContactUs>
    {
        Task<ServiceResponse<List<ContactUs>>> GetAllForLocationAsync(int locationId);
        Task<ServiceResponse<List<string>>> GetDistinctEmail();
        Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId);
        Task<ServiceResponse<ContactUs>> GetByPhone(string phone);
        Task<ServiceResponse<List<string>>> GetDistinctPhone();
        Task<ServiceResponse<List<string>>> GetDistinctPhoneByLocation(int locationId);
        Task<ServiceResponse<List<ContactUs>>> GetAllForLocationList(List<int> locationIds);

    }
}
