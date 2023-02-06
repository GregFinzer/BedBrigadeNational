using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IContentDataService
    {
        Task<ServiceResponse<Content>> CreateAsync(Content content);
        Task<ServiceResponse<bool>> DeleteAsync(int contentId);
        Task<ServiceResponse<List<Content>>> GetAllAsync();
        Task<ServiceResponse<Content>> GetAsync(int contentId);
        Task<ServiceResponse<Content>> GetAsync(string name);
        Task<ServiceResponse<Content>> UpdateAsync(Content content);
    }
}