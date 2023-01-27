using Microsoft.AspNetCore.Components.Authorization;
using BedBrigade.Shared;
using BedBrigade.Admin.Services;

namespace BedBrigade.Client
{
    public class UserServiceFactory : IUserServiceFactory
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authState;
        public UserServiceFactory(HttpClient http, AuthenticationStateProvider authState)
        {
            _http = http;
            _authState = authState;
        }

        public IUserService Create() => new UserService(_http, _authState);
    }
}
