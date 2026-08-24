using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestDataService : IRepository<BedRequest>
    {
        Task<ServiceResponse<List<BedRequest>>> GetAllForLocationAsync(int locationId);
        Task<ServiceResponse<List<BedRequest>>> GetBedRequestsForUser();
        Task<ServiceResponse<List<BedRequest>>> GetAllForLocationList(List<int> locationIds);
        Task<ServiceResponse<List<BedRequest>>> GetScheduledBedRequestsForLocation(int locationId);
        Task<int> MarkInvalidEmailForWaitingForBedRequest(List<string> emailList);
        Task<ServiceResponse<DateTime?>> NextDateEligibleForBedRequest(NewBedRequest bedRequest);
        Task<ServiceResponse<List<BedRequest>>> GetBedRequestsByUserAndStatus(List<BedRequestStatus> statuses);
        Task<ServiceResponse<List<BedRequest>>> GetAllForScheduleId(int scheduleId);
    }
}
