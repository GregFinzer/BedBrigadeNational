using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ITranslationProcessorDataService
    {
        Task QueueContentTranslation(Content content);
        Task ProcessQueue();
    }
}
