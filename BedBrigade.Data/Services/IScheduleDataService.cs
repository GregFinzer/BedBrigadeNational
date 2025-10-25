using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IScheduleDataService : IRepository<Schedule>
    {
        Task<ServiceResponse<List<Schedule>>> GetSchedulesByLocationId(int locationId);
        Task<ServiceResponse<List<Schedule>>> GetFutureSchedulesByLocationId(int locationId);
        Task<ServiceResponse<List<Schedule>>> GetAvailableSchedulesByLocationId(int locationId);
        Task<ServiceResponse<List<Schedule>>> GetScheduleForMonthsAndLocation(int locationId, int numberOfMonthsAway);
        Task UpdateBedRequestSummaryInformation(int locationId, List<BedRequest> scheduledBedRequests);
        Task<ServiceResponse<Schedule?>> GetLastScheduledByLocationId(int locationId);
        Task UpdateScheduleVolunteers(int scheduleId);
    }
}