using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IScheduleDataService : IRepository<Schedule>
    {
        Task<ServiceResponse<List<Schedule>>> GetFutureSchedulesByLocationId(int locationId);
    }
}