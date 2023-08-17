using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IVolunteerDataService : IRepository<Volunteer>
    {
        Task<ServiceResponse<List<Volunteer>>> GetAllForLocationAsync();
    }
}