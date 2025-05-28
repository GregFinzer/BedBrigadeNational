using System.Web;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using Microsoft.AspNetCore.Components;
using Stripe;
using Stripe.Checkout;

namespace BedBrigade.Data.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfigurationDataService _configurationDataService;
        private readonly ICustomSessionService _customSessionService;
        private readonly NavigationManager _navigationManager;
        public PaymentService(IConfigurationDataService configurationDataService, 
            ICustomSessionService customSessionService,
            NavigationManager navigationManager)
        {
            _configurationDataService = configurationDataService;
            _customSessionService = customSessionService;
            _navigationManager = navigationManager;
        }

        private string GetBaseDomain()
        {
            return _navigationManager.BaseUri.TrimEnd('/');
        }

        private async Task<string> CreateSessionId(PaymentSession bookingSession)
        {
            string encryptionKey = await _configurationDataService.GetConfigValueAsync(ConfigSection.Payments, ConfigNames.SessionEncryptionKey);
            string plainText = bookingSession.PaymentSessionId + bookingSession.Email;
            return EncryptionLogic.EncryptString(encryptionKey, plainText);
        }

        public async Task<bool> VerifySessionId(BookingSession bookingSession, string sessionId)
        {
            string encryptionKey = await _configurationDataService.GetConfigValueAsync(ConfigSection.Payments, ConfigNames.SessionEncryptionKey);
            try
            {
                string plainText = EncryptionLogic.DecryptString(encryptionKey, sessionId);
                return plainText == bookingSession.PaymentSessionId + bookingSession.Email;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> GetStripeDepositUrl()
        {
            const string bookingConfirmationPage = "booking-confirmation";
            const string bookingDepositPage = "booking-deposit";

            StripeConfiguration.ApiKey = await _configurationDataService.GetConfigValueAsync(ConfigSection.Payments, ConfigNames.StripeSecretKey);
            decimal amount = await _configurationDataService.GetConfigValueAsDecimalAsync(ConfigSection.Payments, ConfigNames.DepositAmount);
            BookingSession bookingSession = await _customSessionService.GetItemAsync<BookingSession>(GeneralConstants.BookingSessionKey);

            var lineItems = new List<SessionLineItemOptions>();
            lineItems.Add(new SessionLineItemOptions()
            {
                PriceData = new SessionLineItemPriceDataOptions()
                {
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions()
                    {
                        Name = "Booking Deposit"
                    },
                    UnitAmountDecimal = amount * 100
                },
                Quantity = 1
            });

            string sessionId = await CreateSessionId(bookingSession);
            var successUriBuilder = new UriBuilder($"{GetBaseDomain()}/{bookingConfirmationPage}")
            {
                Query = $"sessionid={HttpUtility.UrlEncode(sessionId)}"
            };

            var options = new SessionCreateOptions()
            {
                CustomerEmail = bookingSession.Email,
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = successUriBuilder.ToString(),
                CancelUrl = $"{GetBaseDomain()}/{bookingDepositPage}",
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            return session.Url;
        }
    }
}
