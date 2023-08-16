using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface ILocationDataService : IRepository<Location>
    {
        Task<ServiceResponse<Location>> GetLocationByRouteAsync(string routeName);
        Task<ServiceResponse<List<LocationDistance>>> GetBedBrigadeNearMe(string zipCode);
    }
}