using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IAuthService
    {
        Task<ServiceResponse<User>> ChangePassword(UserChangePassword request);
        Task<bool> IsUserAuthenticated();
        Task<ServiceResponse<string>> Login(UserLogin request);
        Task<ServiceResponse<bool>> RegisterAsync(UserRegister request);
        Task<ServiceResponse<bool>> UpdateAsync(UserRegister request);
    }
}