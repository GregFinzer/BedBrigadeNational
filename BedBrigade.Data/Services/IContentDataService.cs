using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IContentDataService : IRepository<Content>
    {
        Task<ServiceResponse<Content>> GetAsync(string name, int locationId);
        Task<ServiceResponse<List<Content>>> GetAllForLocationAsync();
    }
}