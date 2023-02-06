using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IAuthDataService
    {
        Task<ServiceResponse<bool>> ChangePassword(string userId, string newPassword);
        Task<User> GetUserByEmail(string email);
        Task<ServiceResponse<string>> Login(string email, string password);
        Task<ServiceResponse<bool>> Register(User user, string password);
        Task<bool> UserExists(string email);
    }
}