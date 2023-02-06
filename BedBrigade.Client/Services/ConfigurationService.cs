using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace BedBrigade.Client.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfigurationDataService _data;
        private readonly AuthenticationStateProvider _authState;
        public ConfigurationService(AuthenticationStateProvider authState, IConfigurationDataService dataService)
        {
            _data = dataService;
            _authState = authState;
        }

        public async Task<ServiceResponse<bool>> DeleteConfigAsync(string configKey)
        {
            return await _data.DeleteAsync(configKey);  
        }

        public async Task<ServiceResponse<Configuration>> CreateConfigAsync(Configuration configuration)
        {
            return await _data.CreateAsync(configuration);
        }

        public async Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration)
        {
            return await _data.UpdateAsync(configuration);
        }

        public async Task<ServiceResponse<List<Configuration>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }
    }
}
