using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestEmailDataService : IRepository<BedRequest>
    {
        Task<ServiceResponse<List<string>>> GetDistinctEmail();
        Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId);
        Task<ServiceResponse<List<string>>> EmailsForNotReceivedABed(int locationId);
        Task<ServiceResponse<List<string>>> EmailsForReceivedABed(int locationId);
        Task<ServiceResponse<List<string>>> EmailsForSchedule(int locationId);
        Task<ServiceResponse<BedRequest>> GetWaitingByEmail(string email);
    }
}
