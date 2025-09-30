using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IContentTranslationQueueDataService : IRepository<ContentTranslationQueue>
    {
        Task<ServiceResponse<string>> QueueContentTranslation(ContentTranslationQueue contentTranslation);
        Task<List<ContentTranslationQueue>> GetContentTranslationsToProcess(int maxPerChunk);
        Task<List<ContentTranslationQueue>> GetLockedContentTranslations();
        Task LockContentTranslationsToProcess(List<ContentTranslationQueue> contentTranslationsToProcess);
        Task ClearContentTranslationQueueLock();
        Task DeleteOldContentTranslationQueue(int daysOld);
    }
}
