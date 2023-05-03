using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IContentService
    {
        Task<ServiceResponse<Content>> CreateAsync(Content content);
        Task<ServiceResponse<bool>> DeleteAsync(int contentId);
        Task<ServiceResponse<List<Content>>> GetAllAsync();
        Task<ServiceResponse<Content>> GetAsync(int contentId);
        Task<ServiceResponse<Content>> GetAsync(string name, int locationId);
        Task<ServiceResponse<Content>> UpdateAsync(Content content);
    }
}