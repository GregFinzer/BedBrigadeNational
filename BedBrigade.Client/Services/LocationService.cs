
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client.Services
{
    public class LocationService : ILocationService
    {
        private readonly AuthenticationStateProvider _authState;
        private readonly ILocationDataService _data;

        public LocationService(AuthenticationStateProvider authState, ILocationDataService dataService)
        {
            _authState = authState;
            _data = dataService;
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int locationId)
        {
            return await _data.DeleteAsync(locationId);
        }

        public async Task<ServiceResponse<Location>> GetAsync(int locationId)
        {
            return await _data.GetAsync(locationId);

        }

        public async Task<ServiceResponse<List<Location>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }

        public async Task<ServiceResponse<Location>> UpdateAsync(Location location)
        {
            return await _data.UpdateAsync(location);
        }

        public async Task<ServiceResponse<List<LocationDistance>>> GetBedBrigadeNearMe(string zipCode)
        {
            return await _data.GetBedBrigadeNearMe(zipCode);
        }

        public async Task<ServiceResponse<Location>> CreateAsync(Location location)
        {
            return await _data.CreateAsync(location);
        }

        public async Task<ServiceResponse<Location>> GetLocationByRouteAsync(string route)
        {
            return await _data.GetLocationByRouteAsync(route);
        }
    }
}
