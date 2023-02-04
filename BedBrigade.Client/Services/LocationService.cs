
using BedBrigade.Shared;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace BedBrigade.Client.Services
{
    public class LocationService : ILocationService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authState;

        public LocationService(HttpClient http, AuthenticationStateProvider authState)
        {
            _http = http;
            _authState = authState;
        }

        /// <summary>
        /// Save a Grids value to persist the locations environment
        /// </summary>
        /// <param name="Location"> The DB column to be saved in the Location table</param>
        /// <returns></returns>
        public async Task<ServiceResponse<string>> GetPersistAsync(Common.PersistGrid Location)
        {
            /// ToDo: Add GetPersist codes
            return new ServiceResponse<string>("Persistance", true, "");
        }
        public async Task<ServiceResponse<bool>> SavePersistAsync(Persist persist)
        {
            /// ToDo: Add SavePersist codes
            return new ServiceResponse<bool>("Saved Persistance data", true);
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int locationId)
        {
            return await _http.DeleteFromJsonAsync<ServiceResponse<bool>>($"api/Location/{locationId}");
        }

        public async Task<ServiceResponse<Location>> GetAsync(int locationId)
        {
            return await _http.GetFromJsonAsync<ServiceResponse<Location>>($"api/location/{locationId}");

        }

        public async Task<ServiceResponse<List<Location>>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<ServiceResponse<List<Location>>>($"api/location/GetAll");
        }

        public async Task<ServiceResponse<Location>> UpdateAsync(Location location)
        {
            var result = await _http.PutAsJsonAsync($"api/location", location);
            return await result.Content.ReadFromJsonAsync<ServiceResponse<Location>>();
        }

        public async Task<ServiceResponse<Location>> CreateAsync(Location location)
        {
            var result = await _http.PostAsJsonAsync($"api/location", location);
            return await result.Content.ReadFromJsonAsync<ServiceResponse<Location>>();
        }
    }
}
