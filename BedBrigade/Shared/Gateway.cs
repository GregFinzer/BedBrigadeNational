using System.Net.Http.Json;

namespace BedBrigade.Shared
{
    public class Gateway<T> : IGateway<T> where T : class
    {
        private readonly HttpClient _http;

        public Gateway(HttpClient http)
        {
            _http = http;
        }
        public async Task<ServiceResponse<List<T>>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<ServiceResponse<List<T>>>($"api/{nameof(T)}/getconfiguration");
        }

        public async Task<ServiceResponse<T>> GetAsync(int id)
        {
            return await _http.GetFromJsonAsync<ServiceResponse<T>>($"api/{nameof(T)}/get/{id}");
        }

        public async Task<ServiceResponse<int>> CreateAsync(T objToCreate)
        {
            var result = await _http.PostAsJsonAsync($"api/{nameof(T)}", objToCreate);
            return await result.Content.ReadFromJsonAsync<ServiceResponse<int>>();
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int id)
        {
            return await _http.DeleteFromJsonAsync<ServiceResponse<bool>>($"api/{nameof(T)}/{id}");
        }

        public async Task<ServiceResponse<T>> UpdateAsync(T objToUpdate)
        {
            var result = await _http.PutAsJsonAsync<ServiceResponse<T>>($"api/{nameof(T)}", objToUpdate);
            return await result.Content.ReadFromJsonAsync(ServiceResponse<T>);
        }


    }
}
