using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IVolunteerEventsDataService : IRepository<VolunteerEvent>
    {
        Task<ServiceResponse<List<VolunteerEvent>>> GetAllForLocationAsync();
    }
}