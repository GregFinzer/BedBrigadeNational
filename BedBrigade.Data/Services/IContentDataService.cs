using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IContentDataService : IRepository<Content>
    {
        Task<ServiceResponse<Content>> GetAsync(string name, int locationId);
        Task<ServiceResponse<Content>> GetSingleByLocationAndContentType(int locationId, ContentType contentType);
        Task<ServiceResponse<List<BlogItem>>> GetBlogItems(int locationId, ContentType contentType);
        Task<ServiceResponse<List<BlogItem>>> GetTopBlogItems(int locationId, ContentType contentType);
        Task<ServiceResponse<List<Content>>> GetAllExceptBlogTypes();
        Task<ServiceResponse<List<Content>>> GetForLocationExceptBlogTypes(int locationId);
        Task<ServiceResponse<List<Content>>> GetByContentType(ContentType contentType);
        Task<ServiceResponse<List<Content>>> GetByLocationContentType(int locationId, ContentType contentType);
        
    }
}