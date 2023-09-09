using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IVolunteerEventsDataService : IRepository<VolunteerEvent>
    {
        Task<ServiceResponse<List<VolunteerEvent>>> GetAllForLocationAsync();
    }
}