using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IVolunteerDataService : IRepository<Volunteer>
    {
        Task<ServiceResponse<List<Volunteer>>> GetAllForLocationAsync();
        Task<ServiceResponse<List<string>>> GetDistinctEmail();
        Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId);
        Task<ServiceResponse<List<string>>> GetVolunteerEmailsWithDeliveryVehicles(int locationId);
        Task<ServiceResponse<List<string>>> GetVolunteerEmailsForASchedule(int scheduleId);
        Task<ServiceResponse<Volunteer>> GetByEmail(string email);
    }
}