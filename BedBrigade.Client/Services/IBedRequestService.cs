using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IBedRequestService
    {
        Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest BedRequest);
        Task<ServiceResponse<bool>> DeleteAsync(int BedRequestId);
        Task<ServiceResponse<List<BedRequest>>> GetAllAsync();
        Task<ServiceResponse<BedRequest>> GetAsync(int BedRequestId);
        Task<ServiceResponse<BedRequest>> UpdateAsync(BedRequest BedRequest);
    }
}