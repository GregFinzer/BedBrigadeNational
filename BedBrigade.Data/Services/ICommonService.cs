using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface ICommonService
    {
        Task<ServiceResponse<List<TEntity>>> GetAllForLocationAsync<TEntity>(IRepository<TEntity> repository)
            where TEntity : class, ILocationId;
    }
}
