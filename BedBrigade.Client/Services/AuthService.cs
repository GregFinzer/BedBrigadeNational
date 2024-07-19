using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IAuthDataService _data;

        public AuthService(AuthenticationStateProvider authStateProvider, IAuthDataService dataService)
        {
            _authStateProvider = authStateProvider;
            _data = dataService;
        }

        public async Task<ServiceResponse<User>> ChangePassword(UserChangePassword request)
        {
            return await _data.ChangePassword(request.UserId, request.Password);
        }

        public async Task<bool> IsUserAuthenticated()
        {
            return (await _authStateProvider.GetAuthenticationStateAsync()).User.Identity.IsAuthenticated;
        }

        public async Task<ServiceResponse<string>> Login(UserLogin request)
        {
            return await _data.Login(request.Email, request.Password);
        }

        public async Task<ServiceResponse<bool>> RegisterAsync(UserRegister request)
        {
                return await _data.Register(request.user, request.Password);
        }

        public async Task<ServiceResponse<bool>> UpdateAsync(UserRegister request)
        {
            return await _data.Update(request);
        }
    }
}
