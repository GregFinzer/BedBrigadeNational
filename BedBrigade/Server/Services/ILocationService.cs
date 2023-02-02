using BedBrigade.Shared;

namespace BedBrigade.Server.Services
{
    public interface ILocationService
    {
        Task<ServiceResponse<Location>> CreateAsync(Location location);
        Task<ServiceResponse<bool>> DeleteAsync(string Name);
        Task<ServiceResponse<List<Location>>> GetAllAsync();
        Task<ServiceResponse<Location>> GetAsync(string Name);
        Task<ServiceResponse<Location>> GetAsync(int locationId);
        Task<ServiceResponse<Location>> UpdateAsync(Location location);
    }
}