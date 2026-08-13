using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IEmailQueueDataService : IRepository<EmailQueue>
    {
        Task<List<EmailQueue>> GetLockedEmails();
        Task ClearEmailQueueLock();
        Task<List<EmailQueue>> GetEmailsToProcess(int maxPerChunk);
        Task DeleteOldEmailQueue(int daysOld);
        Task LockEmailsToProcess(List<EmailQueue> emailsToProcess);
        Task<ServiceResponse<string>> GetSendPlanMessage(EmailsToSendParms parms);
        Task<ServiceResponse<List<string>>> GetEmailsToSend(EmailsToSendParms parms);
        Task<ServiceResponse<string>> DeleteQueuedByBedRequestId(int bedRequestId);
        Task<ServiceResponse<bool>> DeleteQueuedBySignUpId(int signUpId);
        Task<ServiceResponse<string>> QueueEmail(EmailQueue email);
        Task<ServiceResponse<string>> QueueBulkEmail(BulkEmailParms bulkEmailParms);
        Task<int> GetEmailsSentTodayCount();
        Task<List<EmailSlim>> GetEmailsSentToday();
        Task<ServiceResponse<List<EmailQueue>>> GetAllForLocationAsync(int locationId);
    }
}
