using BedBrigade.Shared;

namespace BedBrigade.Server.Services.AuthService
{
    public interface IAuthService
    {
        Task<ServiceResponse<string>> Login(string email, string password);
    }
}
