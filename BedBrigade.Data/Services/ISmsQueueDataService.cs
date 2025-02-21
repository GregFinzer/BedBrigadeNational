using BedBrigade.Common.Enums;
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
        Task<ServiceResponse<List<SmsQueueSummary>>> GetSummaryForLocation(int locationId);
        Task<ServiceResponse<List<SmsQueue>>> GetMessagesForLocationAndToPhoneNumber(int locationId, string toPhoneNumber);
        Task<ServiceResponse<SmsQueue>> CreateSmsReply(string fromPhoneNumber, string toPhoneNumber, string body);
        Task FillContactByToPhoneNumber(SmsQueue smsQueue);
        Task MarkMessagesAsRead(int locationId, string toPhoneNumber);

        Task<ServiceResponse<List<string>>> GetPhoneNumbersToSend(int locationId, SmsRecipientOption option, int scheduleId);
        Task<ServiceResponse<string>> QueueBulkSms(int locationId, List<string> phoneNumberList, string body);
        Task<ServiceResponse<string>> GetSendPlanMessage(int locationId, SmsRecipientOption option, int scheduleId);
    }
}
