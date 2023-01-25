using BedBrigade.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using Configuration = BedBrigade.Shared.Configuration;

namespace BedBrigade.Admin.Services
{
    public class ConfigurationService : Gateway<Configuration>
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authState;
        public ConfigurationService(HttpClient http, AuthenticationStateProvider authState) : base(http)
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
    }
}
