using BedBrigade.Data.Models;
using BedBrigade.Data.Shared;

namespace BedBrigade.Client.Services
{
    public interface IContentService
    {
        Task<ServiceResponse<Content>> CreateAsync(Content content);
        Task<ServiceResponse<bool>> DeleteAsync(int contentId);
        Task<ServiceResponse<List<Content>>> GetAllAsync();
        Task<ServiceResponse<Content>> GetAsync(int contentId);
        Task<ServiceResponse<Content>> GetAsync(string name);
        Task<ServiceResponse<string>> GetPersistAsync(Common.PersistGrid Content);
        Task<ServiceResponse<bool>> SavePersistAsync(Persist persist);
        Task<ServiceResponse<Content>> UpdateAsync(Content content);
    }
}