using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace BedBrigade.Client.Services
{
    public class DonationService : IDonationService

    {
        private readonly IDonationDataService _data;
        private readonly AuthenticationStateProvider _authState;
        public DonationService(AuthenticationStateProvider authState, IDonationDataService dataService)
        {
            _data = dataService;
            _authState = authState;
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int donationId)
        {
            return await _data.DeleteAsync(donationId);
        }

        public async Task<ServiceResponse<Donation>> CreateAsync(Donation donation)
        {
            return await _data.CreateAsync(donation);
        }

        public async Task<ServiceResponse<Donation>> UpdateAsync(Donation donation)
        {
            return await _data.UpdateAsync(donation);
        }

        public async Task<ServiceResponse<List<Donation>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }
    }
}
