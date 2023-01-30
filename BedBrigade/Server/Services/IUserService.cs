
using BedBrigade.Shared;

namespace BedBrigade.Server.Services
{
    public interface IUserService
    {
        Task<ServiceResponse<User>> CreateAsync(User user);
        Task<ServiceResponse<bool>> DeleteAsync(string UserName);
        Task<ServiceResponse<User>> GetAsync(string UserName);
        Task<ServiceResponse<List<User>>> GetAllAsync();
        Task<ServiceResponse<User>> UpdateAsync(User user);
    }
}