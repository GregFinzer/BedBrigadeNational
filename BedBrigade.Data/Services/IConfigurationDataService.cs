using BedBrigade.Common.Enums;
using BedBrigade.Data.Models;


namespace BedBrigade.Data.Services
{
    public interface IConfigurationDataService : IRepository<Configuration>
    {
        Task<ServiceResponse<List<Configuration>>> GetAllAsync(ConfigSection section);
    }
}