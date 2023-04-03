using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IAuthDataService
    {
        Task<ServiceResponse<User>> ChangePassword(string userId, string newPassword);
        Task<ServiceResponse<bool>> Update(UserRegister request);
        Task<User> GetUserByEmail(string email);
        Task<ServiceResponse<string>> Login(string email, string password);
        Task<ServiceResponse<bool>> Register(User user, string password);
        Task<bool> UserExists(string email);
    }
}