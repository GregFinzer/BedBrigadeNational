using BedBrigade.Shared;

namespace BedBrigade.Admin.Services
{
    public interface IConfigurationService : IGateway<Configuration>
    {
        Task<ServiceResponse<Configuration>> CreateConfigAsync(Configuration configuration);
        Task<ServiceResponse<bool>> DeleteConfigurationAsync(string configurationKey);
    }
}
