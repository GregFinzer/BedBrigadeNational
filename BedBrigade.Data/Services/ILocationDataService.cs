using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ILocationDataService : IRepository<Location>
    {
        Task<ServiceResponse<Location>> GetLocationByRouteAsync(string routeName);
        Task<ServiceResponse<List<LocationDistance>>> GetBedBrigadeNearMe(string zipCode);
        Task<ServiceResponse<List<Location>>> GetLocationsByMetroAreaId(int metroAreaId);
    }
}