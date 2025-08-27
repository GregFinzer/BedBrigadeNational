using Blazored.SessionStorage;

namespace BedBrigade.Data.Services
{
    /// <summary>
    /// A service to manage the browser session storage using Blazored.SessionStorage
    /// </summary>
    public class CustomSessionService : ICustomSessionService
    {
        private readonly ISessionStorageService _sessionService;

        public CustomSessionService(ISessionStorageService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<string> GetItemAsStringAsync(string key)
        {
            return await _sessionService.GetItemAsStringAsync(key);
        }

        public async Task SetItemAsStringAsync(string key, string value)
        {
            await _sessionService.SetItemAsStringAsync(key, value);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _sessionService.RemoveItemAsync(key);
        }

        public async Task ContainKeyAsync(string key)
        {
            await _sessionService.ContainKeyAsync(key);
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            await _sessionService.SetItemAsync(key, value);
        }

        public async Task<T> GetItemAsync<T>(string key)
        {
            return await _sessionService.GetItemAsync<T>(key);
        }
    }
}
