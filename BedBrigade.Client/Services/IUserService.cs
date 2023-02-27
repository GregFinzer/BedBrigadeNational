using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IUserService
    {
        Task<ServiceResponse<User>> CreateAsync(User user);
        Task<ServiceResponse<bool>> DeleteUserAsync(string userName);
        Task<ServiceResponse<List<User>>> GetAllAsync();
        Task<ServiceResponse<string>> GetPersistAsync(Persist presist);
        Task<ServiceResponse<User>> GetUserAsync(string userName);
        Task<ServiceResponse<bool>> SavePersistAsync(Persist persist);
        Task<ServiceResponse<User>> UpdateAsync(User user);
        Task<ServiceResponse<bool>> UserExists(string email);
        Task<ServiceResponse<List<UserRole>>> GetUserRolesAsync();
        Task <ServiceResponse<List<Role>>> GetRolesAsync();
        Task<ServiceResponse<Role>> GetRoleAsync(int RoleId);
    }
}