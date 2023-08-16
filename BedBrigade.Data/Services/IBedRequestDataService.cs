using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestDataService : IRepository<BedRequest>
    {
        Task<ServiceResponse<List<BedRequest>>> GetAllForLocationAsync();
    }
}