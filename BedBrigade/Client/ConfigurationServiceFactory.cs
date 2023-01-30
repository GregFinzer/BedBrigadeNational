using BedBrigade.Admin.Services;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client
{
    public class ConfigurationServiceFactory : IConfigurationServiceFactory
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authState;

        public ConfigurationServiceFactory(HttpClient http, AuthenticationStateProvider authState)
        {
            _http = http;
            _authState = authState;
        }

        public IConfigurationService Create() => new ConfigurationService(_http, _authState);
    }
}
