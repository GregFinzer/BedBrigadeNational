using BedBrigade.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using Configuration = BedBrigade.Shared.Configuration;

namespace BedBrigade.Client.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authState;
        public ConfigurationService(HttpClient http, AuthenticationStateProvider authState) 
        {
            _http = http;
            _authState = authState;
        }

        public async Task<ServiceResponse<bool>> DeleteConfigAsync(string configKey)
        {
            return await _http.DeleteFromJsonAsync<ServiceResponse<bool>>($"api/configuration/{configKey}");
        }
        public async Task<ServiceResponse<string>> CreateConfigAsync(Configuration objToCreate)
        {
            var result = await _http.PostAsJsonAsync($"api/configuration", objToCreate);
            return await result.Content.ReadFromJsonAsync<ServiceResponse<string>>();
        }

        public async Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration)
        {
            var result = await _http.PutAsJsonAsync($"api/configuration", configuration); 
            return await result.Content.ReadFromJsonAsync<ServiceResponse<Configuration>>();
        }

        public async Task<ServiceResponse<List<Configuration>>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<ServiceResponse<List<Configuration>>>($"api/Configuration/getconfiguration");
        }
    }
}
