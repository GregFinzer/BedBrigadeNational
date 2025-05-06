using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ISubscriptionDataService : IRepository<Subscription>
    {
        Task<ServiceResponse<List<Subscription>>> GetSubscriptionsByNewsletterAsync(int newsletterId);
        Task<ServiceResponse<bool>> Subscribe(int locationId, string newsletterName, string email);
        Task<ServiceResponse<bool>> Unsubscribe(int locationId, string newsletterName, string email);
        Task<ServiceResponse<List<string>>> GetEmailsByNewsletterAsync(int newsletterId);
    }
}
