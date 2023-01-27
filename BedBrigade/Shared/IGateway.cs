namespace BedBrigade.Shared
{
    public interface IGateway<T> where T : class
    {
        Task<ServiceResponse<int>> CreateAsync(T objToCreate);
        Task<ServiceResponse<bool>> DeleteAsync(int id);
        Task<ServiceResponse<List<T>>> GetAllAsync();
        Task<ServiceResponse<T>> GetAsync(int id);
        Task<ServiceResponse<T>> UpdateAsync(T objToUpdate);
    }
}