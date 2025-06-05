using BedBrigade.Common.Models;
using Stripe.Checkout;

namespace BedBrigade.Data.Services
{
    public interface IPaymentService
    {
        Task<bool> VerifySessionId(PaymentSession paymentSession, string sessionId);
        Task<ServiceResponse<string>> GetStripeDepositUrl(PaymentSession paymentSession);
        Task<ServiceResponse<(decimal gross, decimal fee)>> GetStripeTransactionDetails(string sessionId);
        Task<Session?> GetStripeSession(string sessionId);
        Task<ServiceResponse<Donation>> CreateDonationRecordFromPaymentSession(PaymentSession paymentSession,
            Session stripeSession, decimal fee);
    }
}
