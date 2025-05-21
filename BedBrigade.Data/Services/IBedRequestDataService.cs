using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestDataService : IRepository<BedRequest>
    {
        Task<ServiceResponse<List<BedRequest>>> GetAllForLocationAsync(int locationId);
        Task<ServiceResponse<List<string>>> GetDistinctEmail();
        Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId);
        Task<ServiceResponse<List<string>>> EmailsForNotReceivedABed(int locationId);
        Task<ServiceResponse<List<string>>> EmailsForReceivedABed(int locationId);
        Task<ServiceResponse<List<string>>> EmailsForSchedule(int locationId);
        Task<ServiceResponse<List<BedRequest>>> GetAllForLocationList(List<int> locationIds);
        Task<ServiceResponse<List<BedRequest>>> GetScheduledBedRequestsForLocation(int locationId);
        Task<ServiceResponse<int>> SumBedsForNotReceived(int locationId);
        Task<ServiceResponse<BedRequest>> GetByPhone(string phone);
        Task<ServiceResponse<List<string>>> GetDistinctPhone();
        Task<ServiceResponse<List<string>>> GetDistinctPhoneByLocation(int locationId);
        Task<ServiceResponse<List<string>>> PhonesForNotReceivedABed(int locationId);
        Task<ServiceResponse<List<string>>> PhonesForReceivedABed(int locationId);
        Task<ServiceResponse<List<string>>> PhonesForSchedule(int locationId);
        List<BedRequest> SortBedRequestClosestToAddress(List<BedRequest> bedRequests, int bedRequestId);
    }
}