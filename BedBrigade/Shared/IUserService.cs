using BedBrigade.Shared;
using static BedBrigade.Shared.Common;

namespace BedBrigade.Shared
{
    public interface IUserService 
    {
        Task<ServiceResponse<string>> GetPersistAsync(PersistGrid user);
        Task<ServiceResponse<bool>> SavePersistAsync(Persist persist);
        Task<ServiceResponse<bool>> DeleteUserAsync(string userName);
        Task<ServiceResponse<bool>> RegisterUserAsync(User user);
        Task<ServiceResponse<User>> GetUserAsync(string userName);
        Task<ServiceResponse<List<User>>> GetAllAsync();
        Task<ServiceResponse<User>> UpdateAsync(User user);
        Task<ServiceResponse<User>> CreateAsync(User user);
    }
}