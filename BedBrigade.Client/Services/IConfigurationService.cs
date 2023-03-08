using BedBrigade.Data.Models;
using static BedBrigade.Common.Common;

namespace BedBrigade.Client.Services
{
    public interface IConfigurationService
    {
        Task<ServiceResponse<Configuration>> CreateConfigAsync(Configuration objToCreate);
        Task<ServiceResponse<bool>> DeleteConfigAsync(string configKey);
        Task<ServiceResponse<List<Configuration>>> GetAllAsync();
        Task<ServiceResponse<List<Configuration>>> GetAllAsync(ConfigSection section);
        Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration);
    }
}