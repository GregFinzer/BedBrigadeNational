using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface ITemplateDataService
    {
        Task<ServiceResponse<Template>> CreateAsync(Template template);
        Task<ServiceResponse<bool>> DeleteAsync(int contentId);
        Task<ServiceResponse<List<Template>>> GetAllAsync();
        Task<ServiceResponse<List<Template>>> GetAllAsync(Common.Common.ContentType type);
        Task<ServiceResponse<Template>> GetAsync(int contentId);
        Task<ServiceResponse<Template>> GetAsync(string name);
        Task<ServiceResponse<Template>> UpdateAsync(Template template);
    }
}
