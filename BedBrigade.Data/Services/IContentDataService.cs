using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IContentDataService : IRepository<Content>
    {
        Task<ServiceResponse<Content>> GetAsync(string name, int locationId);
    }
}