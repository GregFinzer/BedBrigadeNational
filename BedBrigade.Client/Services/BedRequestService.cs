
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client.Services
{
    public class BedRequestService : IBedRequestService
    {
        private readonly AuthenticationStateProvider _authState;
        private readonly IBedRequestDataService _data;

        public BedRequestService(AuthenticationStateProvider authState, IBedRequestDataService dataService)
        {
            _authState = authState;
            _data = dataService;
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int BedRequestId)
        {
            return await _data.DeleteAsync(BedRequestId);
        }

        public async Task<ServiceResponse<BedRequest>> GetAsync(int BedRequestId)
        {
            return await _data.GetAsync(BedRequestId);

        }

        public async Task<ServiceResponse<List<BedRequest>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }

        public async Task<ServiceResponse<BedRequest>> UpdateAsync(BedRequest BedRequest)
        {
            return await _data.UpdateAsync(BedRequest);
        }

        public async Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest BedRequest)
        {
            return await _data.CreateAsync(BedRequest);
        }
    }
}
