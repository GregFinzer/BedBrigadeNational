using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestFailedDeliveryDataService : IRepository<BedRequest>
    {
        Task<ServiceResponse<List<BedRequest>>> GetReplacementBedRequests(BedRequest bedRequest);
        Task<ServiceResponse<List<BedRequest>>> GetWaitingForLocation(int locationId);
    }
}
