using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestEstimatedWaitDataService : IRepository<BedRequest>
    {
        Task<ServiceResponse<string>> GetEstimatedWaitTime(int locationId);
        Task<EstimatedWaitResult> GetEstimatedWaitResult(int locationId, DateTime maximumBedRequestDate);
    }
}
