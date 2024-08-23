using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IRepository<TEntity> where TEntity : class
    {
        //Get stuff out of the identity
        Task<string?> GetUserEmail();

        Task<string?> GetUserName();

        string GetEntityName();

        Task<int> GetUserLocationId();

        Task<string?> GetUserRole();
        
        //Entity Framework Wrappers
        Task<ServiceResponse<List<TEntity>>> GetAllAsync();
        Task<ServiceResponse<TEntity>> GetByIdAsync(object id);
        Task<ServiceResponse<TEntity>> CreateAsync(TEntity entity);
        Task<ServiceResponse<TEntity>> UpdateAsync(TEntity entity);
        Task<ServiceResponse<bool>> DeleteAsync(object id);
    }
}
