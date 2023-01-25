
using BedBrigade.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BedBrigade.Admin.Services
{
    public class UserService : Gateway<User>, IUserService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authState;
        public UserService(HttpClient http, AuthenticationStateProvider authState) : base(http)
        {
            _http = http;
            _authState = authState;
        }

        /// <summary>
        /// Save a Grids value to persist the users environment
        /// </summary>
        /// <param name="User"> The DB column to be saved in the User table</param>
        /// <returns></returns>
        public async Task<ServiceResponse<string>> GetPersistAsync(Common.PresistGrid User)
        {
            /// ToDo: Add GetPersiste codes
            return new ServiceResponse<string> { Success = true, Data = string.Empty };
        }

        public async Task<ServiceResponse<bool>> SavePersistAsync(Persist persist)
        {
            /// ToDo: Add SavePersiste codes
            return new ServiceResponse<bool> { Success = true, Data = true };
        }

        public async Task<ServiceResponse<bool>> DeleteUserAsync(string userName)
        {
            return await _http.DeleteFromJsonAsync<ServiceResponse<bool>>($"api/User/{userName}");
        }

        public async Task<ServiceResponse<bool>> RegisterUserAsync(User user)
        {
            return await _http.RegisterUserAsync<ServiceResponse<bool>> ($"api/User/register", user);
        }
    }
}
