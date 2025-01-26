using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ISmsQueueDataService : IRepository<SmsQueue>
    {
        Task<List<SmsQueue>> GetLockedMessages();
        Task ClearSmsQueueLock();
        Task<List<SmsQueue>> GetMessagesToProcess(int maxPerChunk);
        Task DeleteOldSmsQueue(int daysOld);
        Task LockMessagesToProcess(List<SmsQueue> messagesToProcess);
        //Task<ServiceResponse<bool>> DeleteByAppointmentId(int appointmentId);
    }
}
