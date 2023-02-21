using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestDataService
    {
        Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest bedRequest);
        Task<ServiceResponse<bool>> DeleteAsync(int bedRequestId);
        Task<ServiceResponse<List<BedRequest>>> GetAllAsync();
        Task<ServiceResponse<BedRequest>> GetAsync(int bedRequestId);
        Task<ServiceResponse<BedRequest>> UpdateAsync(BedRequest bedRequest);
    }
}