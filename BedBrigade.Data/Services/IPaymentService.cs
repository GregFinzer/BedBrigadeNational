using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IPaymentService
    {
        Task<ServiceResponse<string>> GetStripeDepositUrl();
        //Task<bool> VerifySessionId(BookingSession bookingSession, string sessionId);
    }
}
