using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IUserPersistDataService : IRepository<UserPersist>
    {
        Task<ServiceResponse<bool>> SaveGridPersistence(UserPersist persist);
        Task<ServiceResponse<string>> GetGridPersistence(UserPersist persist);

    }
}
