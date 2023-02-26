
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

        public async Task<ServiceResponse<bool>> DeleteAsync(int volunteerId)
        {
            return await _data.DeleteAsync(volunteerId);
        }

        public async Task<ServiceResponse<Volunteer>> GetAsync(int volunteerId)
        {
            return await _data.GetAsync(volunteerId);

        }

        public async Task<ServiceResponse<List<Volunteer>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }

        public async Task<ServiceResponse<Volunteer>> UpdateAsync(Volunteer volunteer)
        {
            return await _data.UpdateAsync(volunteer);
        }

        public async Task<ServiceResponse<Volunteer>> CreateAsync(Volunteer volunteer)
        {
            return await _data.CreateAsync(volunteer);
        }
    }
}
