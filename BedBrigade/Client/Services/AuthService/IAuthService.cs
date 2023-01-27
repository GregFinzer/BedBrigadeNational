using BedBrigade.Shared;

namespace BedBrigade.Client.Services
{
    public interface IAuthService
    {
        Task<ServiceResponse<bool>> Register(UserRegister request);
        Task<ServiceResponse<string>> Login(UserLogin request);
        Task<ServiceResponse<bool>> ChangePassword(UserChangePassword request);
        Task<bool> IsUserAuthenticated();
    }
}
