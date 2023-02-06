
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using BedBrigade.Data.Shared;
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

        public async Task<ServiceResponse<Location>> CreateAsync(Location location)
        {
            return await _data.CreateAsync(location);
        }
    }
}
