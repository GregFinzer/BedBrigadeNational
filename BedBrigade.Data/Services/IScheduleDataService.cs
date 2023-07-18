using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IScheduleDataService // VS 07/05/2023
    {
        Task<ServiceResponse<Schedule>> CreateAsync(Schedule schedule);
        Task<ServiceResponse<bool>> DeleteAsync(int ScheduleId);
        Task<ServiceResponse<List<Schedule>>> GetAllAsync();
        Task<ServiceResponse<Schedule>> GetAsync(int ScheduleId);
        Task<ServiceResponse<Schedule>> UpdateAsync(Schedule schedule);
       
    }
}