using BedBrigade.Shared;

namespace BedBrigade.Admin.Services
{
    public interface IConfigurationService 
    {
        Task<ServiceResponse<string>> CreateConfigAsync(Configuration objToCreate);
        Task<ServiceResponse<bool>> DeleteConfigAsync(string configKey);
        Task<ServiceResponse<List<Configuration>>> GetAllAsync();
        Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration);
    }
}