using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IUserService
    {
        Task<ServiceResponse<User>> CreateAsync(User user);
        Task<ServiceResponse<bool>> DeleteUserAsync(string userName);
        Task<ServiceResponse<List<User>>> GetAllAsync();
        Task<ServiceResponse<string>> GetPersistAsync(Common.Common.PersistGrid User);
        Task<ServiceResponse<User>> GetUserAsync(string userName);
        Task<ServiceResponse<bool>> SavePersistAsync(Persist persist);
        Task<ServiceResponse<User>> UpdateAsync(User user);
        Task<ServiceResponse<bool>> UserExists(string email);
    }
}