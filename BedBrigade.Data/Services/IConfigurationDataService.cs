using BedBrigade.Data.Models;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Services
{
    public interface IConfigurationDataService
    {
        Task<ServiceResponse<Configuration>> CreateAsync(Configuration configuration);
        Task<ServiceResponse<bool>> DeleteAsync(string configurationKey);
        Task<ServiceResponse<List<Configuration>>> GetAllAsync();
        Task<ServiceResponse<List<Configuration>>> GetAllAsync(ConfigSection section);
        Task<ServiceResponse<Configuration>> GetAsync(string configurationKey);
        Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration);
    }
}