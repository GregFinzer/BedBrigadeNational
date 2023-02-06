using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IConfigurationService
    {
        Task<ServiceResponse<Configuration>> CreateConfigAsync(Configuration objToCreate);
        Task<ServiceResponse<bool>> DeleteConfigAsync(string configKey);
        Task<ServiceResponse<List<Configuration>>> GetAllAsync();
        Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration);
    }
}