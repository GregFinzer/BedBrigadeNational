using BedBrigade.Data.Models;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Services
{
    public interface IConfigurationDataService : IRepository<Configuration>
    {
        Task<ServiceResponse<List<Configuration>>> GetAllAsync(ConfigSection section);
    }
}