
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client.Services
{
    public class VolunteerService : IVolunteerService
    {
        private readonly AuthenticationStateProvider _authState;
        private readonly IVolunteerDataService _data;

        public VolunteerService(AuthenticationStateProvider authState, IVolunteerDataService dataService)
        {
            _authState = authState;
            _data = dataService;
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int VolunteerId)
        {
            return await _data.DeleteAsync(VolunteerId);
        }

        public async Task<ServiceResponse<Volunteer>> GetAsync(int locationId)
        {
            return await _data.GetAsync(locationId);

        }

        public async Task<ServiceResponse<List<Volunteer>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }

        public async Task<ServiceResponse<Volunteer>> UpdateAsync(Volunteer location)
        {
            return await _data.UpdateAsync(location);
        }

        public async Task<ServiceResponse<Volunteer>> CreateAsync(Volunteer location)
        {
            return await _data.CreateAsync(location);
        }
    }
}
