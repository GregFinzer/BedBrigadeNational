using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IPaymentService
    {
        Task<string> GetStripeDepositUrl();
        //Task<bool> VerifySessionId(BookingSession bookingSession, string sessionId);
    }
}
