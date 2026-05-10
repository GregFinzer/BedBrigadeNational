using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ITranslationQueueDataService : IRepository<TranslationQueue>
    {
        Task<ServiceResponse<string>> QueueTranslation(TranslationQueue translation);
        Task<List<TranslationQueue>> GetTranslationsToProcess(int maxPerChunk);
        Task<List<TranslationQueue>> GetLockedTranslations();
        Task LockTranslationsToProcess(List<TranslationQueue> translationsToProcess);
        Task ClearTranslationQueueLock();
        Task DeleteOldTranslationQueue(int daysOld);
        Task<List<TranslationQueueView>> GetTranslationQueueView();
    }
}
