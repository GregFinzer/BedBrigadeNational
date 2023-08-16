using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface ITemplateDataService : IRepository<Template>
    {
        Task<ServiceResponse<Template>> GetByNameAsync(string name);
        Task<ServiceResponse<List<Template>>> GetByContentTypeAsync(Common.Common.ContentType type);
    }
}
