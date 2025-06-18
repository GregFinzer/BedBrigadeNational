using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IRepository<TEntity> where TEntity : class
    {
        //Get stuff out of the identity
        string? GetUserEmail();

        string GetUserName();

        string GetEntityName();

        int GetUserLocationId();

        string? GetUserRole();

        bool IsUserNationalAdmin();

        //Entity Framework Wrappers
        Task<ServiceResponse<List<TEntity>>> GetAllAsync();
        Task<ServiceResponse<TEntity>> GetByIdAsync(object id);
        Task<ServiceResponse<TEntity>> CreateAsync(TEntity entity);
        Task<ServiceResponse<TEntity>> UpdateAsync(TEntity entity);
        Task<ServiceResponse<bool>> DeleteAsync(object id);
    }
}
