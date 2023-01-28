using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client
{
    public class ConfigurationServiceFactory
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authState;

        public ConfigurationServiceFactory(HttpClient http, AuthenticationStateProvider authState)
        {
            _http = http;
            _authState = authState;
        }
    }
}
