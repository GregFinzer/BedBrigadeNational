using BedBrigade.Shared;
using static BedBrigade.Shared.Common;

namespace BedBrigade.Admin.Services
{
    public interface IUserService : IGateway<User>
    {
        Task<ServiceResponse<string>> GetPersistAsync(PersistGrid user);
        Task<ServiceResponse<bool>> SavePersistAsync(Persist persist);
        Task<ServiceResponse<bool>> DeleteUserAsync(string userName);
        Task<ServiceResponse<bool>> RegisterUserAsync(User user);
    }
}