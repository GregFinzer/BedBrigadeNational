using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface INewsletterDataService : IRepository<Newsletter>
    {
        Task<ServiceResponse<Newsletter>> GetAsync(string name, int locationId);
        Task<ServiceResponse<List<Newsletter>>> GetAllForLocationAsync(int locationId);
    }
}
