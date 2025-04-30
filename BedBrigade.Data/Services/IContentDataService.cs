using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IContentDataService : IRepository<Content>
    {
        Task<ServiceResponse<Content>> GetAsync(string name, int locationId);
        Task<ServiceResponse<List<Content>>> GetAllForLocationAsync(int locationId);
        Task<ServiceResponse<Content>> GetByLocationAndContentType(int locationId, ContentType contentType);
        Task<ServiceResponse<List<BlogItemNew>>> GetBlogItems(int locationId, ContentType contentType);
    }
}