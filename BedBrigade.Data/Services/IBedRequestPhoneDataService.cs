using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestPhoneDataService : IRepository<BedRequest>
    {
        Task<ServiceResponse<BedRequest>> GetByPhone(string phone);
        Task<ServiceResponse<List<string>>> GetDistinctPhone();
        Task<ServiceResponse<List<string>>> GetDistinctPhoneByLocation(int locationId);
        Task<ServiceResponse<List<string>>> PhonesForNotReceivedABed(int locationId);
        Task<ServiceResponse<List<string>>> PhonesForReceivedABed(int locationId);
        Task<ServiceResponse<List<string>>> PhonesForSchedule(int locationId);
        Task<ServiceResponse<BedRequest>> GetWaitingByPhone(string phone);
    }
}
