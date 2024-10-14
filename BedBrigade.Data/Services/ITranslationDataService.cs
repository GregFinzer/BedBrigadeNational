using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ITranslationDataService : IRepository<Translation>
    {
        Task<ServiceResponse<List<Translation>>> GetTranslationsForLanguage(string languageCode);
    }
}
