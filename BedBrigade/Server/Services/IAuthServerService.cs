using BedBrigade.Shared;

namespace BedBrigade.Server.Services
{
    public interface IAuthService
    {
        Task<ServiceResponse<bool>> ChangePassword(int userId, string newPassword);
        Task<User> GetUserByEmail(string email);
        string GetUserEmail();
        int GetUserId();
        Task<ServiceResponse<string>> Login(string email, string password);
        Task<ServiceResponse<bool>> Register(User user, string password);
        Task<bool> UserExists(string email);
    }
}