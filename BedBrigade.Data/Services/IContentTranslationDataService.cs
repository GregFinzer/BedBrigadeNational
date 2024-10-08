using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IContentTranslationDataService : IRepository<ContentTranslation>
    {
        Task<ServiceResponse<ContentTranslation>> GetAsync(string name, int locationId, string culture);
    }
}
