using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IGeoLocationQueueDataService : IRepository<GeoLocationQueue>
    {
        Task<List<GeoLocationQueue>> GetGeoLocationsToday();
        Task<List<GeoLocationQueue>> GetLockedGeoLocationQueue();
        Task ClearGeoLocationQueueLock();
        Task<List<GeoLocationQueue>> GetItemsToProcess();
        Task LockItemsToProcess(List<GeoLocationQueue> itemsToProcess);
        Task DeleteOldGeoLocationQueue(int daysOld);
    }
}
