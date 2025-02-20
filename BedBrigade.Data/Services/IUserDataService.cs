using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IUserDataService : IRepository<User>
    {
        Task<ServiceResponse<User>> GetCurrentLoggedInUser();
        Task<ServiceResponse<List<Role>>> GetRolesAsync();
        Task<ServiceResponse<Role>> GetRoleAsync(int roleId);
        Task<ServiceResponse<List<User>>> GetAllForLocationAsync(int locationId);
        Task<ServiceResponse<List<string>>> GetDistinctEmail();
        Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId);
        Task<ServiceResponse<string>> GetEmailSignature(string userName);
        Task<ServiceResponse<User>> GetByPhone(string phone);
        Task<ServiceResponse<List<string>>> GetDistinctPhone();
        Task<ServiceResponse<List<string>>> GetDistinctPhoneByLocation(int locationId);

    }
}