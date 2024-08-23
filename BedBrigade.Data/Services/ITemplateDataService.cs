using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ITemplateDataService : IRepository<Template>
    {
        Task<ServiceResponse<Template>> GetByNameAsync(string name);
        Task<ServiceResponse<List<Template>>> GetByContentTypeAsync(ContentType type);
    }
}
