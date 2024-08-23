using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;


namespace BedBrigade.Data.Services
{
    public interface IConfigurationDataService : IRepository<Configuration>
    {
        Task<ServiceResponse<List<Configuration>>> GetAllAsync(ConfigSection section);
        Task<int> GetConfigValueAsIntAsync(ConfigSection section, string key);
        Task<string> GetConfigValueAsync(ConfigSection section, string key);
        Task<decimal> GetConfigValueAsDecimalAsync(ConfigSection section, string key);
        Task<bool> GetConfigValueAsBoolAsync(ConfigSection section, string key);
    }
}