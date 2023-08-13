using BedBrigade.Data.Models;
using ContentType = BedBrigade.Common.Common.ContentType;

namespace BedBrigade.Data.Services
{
    public interface IContentDataService : IRepository<Content>
    {
        //Task<ServiceResponse<Content>> CreateAsync(Content content);
        //Task<ServiceResponse<bool>> DeleteAsync(int contentId);
        //Task<ServiceResponse<List<Content>>> GetAllAsync();
        //Task<ServiceResponse<List<Content>>> GetAllAsync(ContentType type, int locationId);
        //Task<ServiceResponse<Content>> GetAsync(int contentId);
        //Task<ServiceResponse<Content>> GetAsync(string name);
        Task<ServiceResponse<Content>> GetAsync(string name, int locationId);
        //Task<ServiceResponse<Content>> UpdateAsync(Content content);
    }
}