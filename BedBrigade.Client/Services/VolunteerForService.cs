
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client.Services
{
    public class VolunteerForService : IVolunteerForService
    {
        private readonly AuthenticationStateProvider _authState;
        private readonly IVolunteerForDataService _data;

        public VolunteerForService(AuthenticationStateProvider authState, IVolunteerForDataService dataService)
        {
            _authState = authState;
            _data = dataService;
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int VolunteerForId)
        {
            return await _data.DeleteAsync(VolunteerForId);
        }

        public async Task<ServiceResponse<VolunteerFor>> GetAsync(int VolunteerForId)
        {
            return await _data.GetAsync(VolunteerForId);

        }

        public async Task<ServiceResponse<List<VolunteerFor>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }

        public async Task<ServiceResponse<VolunteerFor>> UpdateAsync(VolunteerFor volunteerFor)
        {
            return await _data.UpdateAsync(volunteerFor);
        }

        public async Task<ServiceResponse<VolunteerFor>> CreateAsync(VolunteerFor volunteerFor)
        {
            return await _data.CreateAsync(volunteerFor);
        }
    }
}
