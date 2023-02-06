using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IConfigurationDataService
    {
        Task<ServiceResponse<Configuration>> CreateAsync(Configuration configuration);
        Task<ServiceResponse<bool>> DeleteAsync(string UserName);
        Task<ServiceResponse<List<Configuration>>> GetAllAsync();
        Task<ServiceResponse<Configuration>> GetAsync(string configurationKey);
        Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration);
    }
}