
using BedBrigade.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace BedBrigade.Admin.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authState;
        public UserService(HttpClient http, AuthenticationStateProvider authState)
        {
            _http = http;
            _authState = authState;
        }

        /// <summary>
        /// Save a Grids value to persist the users environment
        /// </summary>
        /// <param name="User"> The DB column to be saved in the User table</param>
        /// <returns></returns>
        public async Task<ServiceResponse<string>> GetPersistAsync(Common.PersistGrid User)
        {
            /// ToDo: Add GetPersist codes
            return new ServiceResponse<string>("Persistance", true, "");
        }
        public async Task<ServiceResponse<bool>> SavePersistAsync(Persist persist)
        {
            /// ToDo: Add SavePersist codes
            return new ServiceResponse<bool>("Saved Persistance data", true);
        }

        public async Task<ServiceResponse<bool>> DeleteUserAsync(string userName)
        {
            return await _http.DeleteFromJsonAsync<ServiceResponse<bool>>($"api/User/{userName}");
        }

        public async Task<ServiceResponse<bool>> RegisterUserAsync(User user)
        {
            var result = await _http.PostAsJsonAsync($"api/user/register", user);
            return await result.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        }

        public async Task<ServiceResponse<User>> GetUserAsync(string userName)
        {
            return await _http.GetFromJsonAsync<ServiceResponse<User>>($"api/user/getuser/{userName}");

        }

        public async Task<ServiceResponse<List<User>>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<ServiceResponse<List<User>>>($"api/user/getall");
        }

        public async Task<ServiceResponse<User>> UpdateAsync(User user)
        {
            var result = await _http.PutAsJsonAsync($"api/user", user);
            return await result.Content.ReadFromJsonAsync<ServiceResponse<User>>();
        }

        public async Task<ServiceResponse<User>> CreateAsync(User user)
        {
            var result = await _http.PostAsJsonAsync($"api/user", user);
            return await result.Content.ReadFromJsonAsync<ServiceResponse<User>>();
        }
    }
}
