using System;
using System.Collections.Generic;
using System.Web;
using System.Threading.Tasks;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using KellermanSoftware.NetEmailValidation;
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
        private readonly ILocationDataService _locationDataService;
        private readonly IDonationDataService _donationDataService;
        public PaymentService(
            IConfigurationDataService configurationDataService,
            ICustomSessionService customSessionService,
            NavigationManager navigationManager,
            ILocationDataService locationDataService, 
            IDonationDataService donationDataService)
        {
            _configurationDataService = configurationDataService;
            _customSessionService = customSessionService;
            _navigationManager = navigationManager;
            _locationDataService = locationDataService;
            _donationDataService = donationDataService;
        }

        private string GetBaseDomain()
        {
            return _navigationManager.BaseUri.TrimEnd('/');
        }

        private async Task<string> CreateSessionId(PaymentSession paymentSession)
        {
            string encryptionKey = await _configurationDataService
                .GetConfigValueAsync(ConfigSection.Payments, ConfigNames.SessionEncryptionKey);
            string plainText = paymentSession.PaymentSessionId.ToString();
            return EncryptionLogic.EncryptString(encryptionKey, plainText);
        }

        public async Task<bool> VerifySessionId(PaymentSession paymentSession, string sessionId)
        {
            string encryptionKey = await _configurationDataService
                .GetConfigValueAsync(ConfigSection.Payments, ConfigNames.SessionEncryptionKey);
            try
            {
                string plainText = EncryptionLogic.DecryptString(encryptionKey, sessionId);
                return plainText == paymentSession.PaymentSessionId.ToString();
            }
            catch
            {
                return false;
            }
        }

        public async Task<ServiceResponse<string>> GetStripeDepositUrl(PaymentSession paymentSession)
        {
            const string donationConfirmationPage = "donation-confirmation";
            const string donationCancellationPage = "donation-cancellation";

            // Retrieve and validate Stripe secret key
            ServiceResponse<string> secretKeyResponse = await GetStripeSecretKeyAsync();
            if (!secretKeyResponse.Success || secretKeyResponse.Data == null)
                return new ServiceResponse<string>(secretKeyResponse.Message, false);
            StripeConfiguration.ApiKey = secretKeyResponse.Data!;

            // Validate PaymentSession
            string validationResult = ValidatePaymentSession(paymentSession);

            if (!string.IsNullOrEmpty(validationResult))
                return new ServiceResponse<string>(validationResult, false);

            // Retrieve and validate Location
            ServiceResponse<Location> locationResponse = await _locationDataService
                .GetByIdAsync(paymentSession.LocationId.Value);
            if (!locationResponse.Success || locationResponse.Data == null)
                return new ServiceResponse<string>(
                    $"Location not found for ID {paymentSession.LocationId}", false);

            // Build RequestOptions (StripeAccount per location)
            RequestOptions requestOptions = await BuildRequestOptionsAsync(paymentSession.LocationId.Value);

            // Build line items (one�time vs. subscription)
            List<SessionLineItemOptions> lineItems = BuildLineItems(paymentSession);

            // Create encrypted session ID
            string sessionId = await CreateSessionId(paymentSession);

            // Build SessionCreateOptions
            SessionCreateOptions options = BuildSessionCreateOptions(
                paymentSession,
                lineItems,
                sessionId,
                donationConfirmationPage,
                donationCancellationPage);

            // Call Stripe to create the Checkout Session
            SessionService service = new SessionService();
            Session? session = await service.CreateAsync(options, requestOptions);

            if (session == null || string.IsNullOrEmpty(session.Url))
            {
                return new ServiceResponse<string>("Failed to create Stripe session.", false);
            }

            paymentSession.StripeSessionId = session.Id;

            return new ServiceResponse<string>("Valid", true, session.Url);
        }

        private async Task<ServiceResponse<string>> GetStripeSecretKeyAsync()
        {
            string key = await _configurationDataService
                .GetConfigValueAsync(ConfigSection.Payments, ConfigNames.StripeSecretKey);
            if (string.IsNullOrEmpty(key))
                return new ServiceResponse<string>("Stripe secret key is not configured.", false);
            return new ServiceResponse<string>("Valid", true, key);
        }



        private async Task<RequestOptions> BuildRequestOptionsAsync(int locationId)
        {
            string accountId = await _configurationDataService
                .GetConfigValueAsync(ConfigSection.Payments, ConfigNames.StripeAccountId, locationId);
            return new RequestOptions { StripeAccount = accountId };
        }

        private List<SessionLineItemOptions> BuildLineItems(PaymentSession paymentSession)
        {
            List<SessionLineItemOptions> items = new List<SessionLineItemOptions>();

            if (paymentSession.SubscriptionAmount.HasValue
                && paymentSession.SubscriptionAmount.Value > 0)
            {
                items.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = "month"
                        },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Monthly Recurring Donation"
                        },
                        UnitAmountDecimal = paymentSession.SubscriptionAmount.Value * 100m
                    },
                    Quantity = 1
                });
            }
            else
            {
                // One time donation
                items.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Charitable Donation"
                        },
                        UnitAmountDecimal = paymentSession.DonationAmount!.Value * 100m
                    },
                    Quantity = 1
                });
            }

            return items;
        }

        private SessionCreateOptions BuildSessionCreateOptions(
            PaymentSession paymentSession,
            List<SessionLineItemOptions> lineItems,
            string sessionId,
            string successPage,
            string cancelPage)
        {
            string successUrl = new UriBuilder($"{GetBaseDomain()}/{successPage}")
            {
                Query = $"sessionid={HttpUtility.UrlEncode(sessionId)}"
            }.ToString();

            return new SessionCreateOptions
            {
                CustomerEmail = paymentSession.Email,
                ClientReferenceId = paymentSession.PaymentSessionId.ToString(),
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = paymentSession.SubscriptionAmount.HasValue
                       && paymentSession.SubscriptionAmount.Value > 0
                    ? "subscription"
                    : "payment",
                SuccessUrl = successUrl,
                CancelUrl = $"{GetBaseDomain()}/{cancelPage}"
            };
        }

        private string ValidatePaymentSession(PaymentSession paymentSession)
        {
            if (!paymentSession.LocationId.HasValue)
                return "Location ID is required.";

            if (string.IsNullOrEmpty(paymentSession.FirstName))
                return "First Name is required.";

            if (string.IsNullOrEmpty(paymentSession.LastName))
                return "Last Name is required.";

            Result emailCheck = Validation.IsValidEmail(paymentSession.Email);
            if (!emailCheck.IsValid)
                return emailCheck.UserMessage;

            if (string.IsNullOrEmpty(paymentSession.PhoneNumber)
                || !Validation.IsValidPhoneNumber(paymentSession.PhoneNumber))
                return "A valid phone number is required.";

            if (!paymentSession.DonationAmount.HasValue
                && !paymentSession.SubscriptionAmount.HasValue)
                return "Donation amount or subscription amount must be provided.";

            if (paymentSession.DonationAmount <= 0
                && paymentSession.SubscriptionAmount <= 0)
                return "Donation amount or subscription amount must be greater than zero.";

            return string.Empty;
        }

        public async Task<ServiceResponse<(decimal gross, decimal fee)>> GetStripeTransactionDetails(string sessionId)
        {
            try
            {
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(sessionId);
                if (session == null)
                    return new ServiceResponse<(decimal gross, decimal fee)>("Session not found", false);

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.GetAsync(session.PaymentIntentId);
                if (paymentIntent == null || string.IsNullOrEmpty(paymentIntent.LatestChargeId))
                    return new ServiceResponse<(decimal gross, decimal fee)>("Payment details not found", false);

                var chargeService = new ChargeService();
                var charge = await chargeService.GetAsync(paymentIntent.LatestChargeId);
                if (charge == null || string.IsNullOrEmpty(charge.BalanceTransactionId))
                    return new ServiceResponse<(decimal gross, decimal fee)>("Charge or balance transaction not found", false);

                decimal gross = charge.Amount / 100M;

                var balanceTransactionService = new BalanceTransactionService();
                var balanceTransaction = await balanceTransactionService.GetAsync(charge.BalanceTransactionId);
                if (balanceTransaction == null)
                    return new ServiceResponse<(decimal gross, decimal fee)>("Balance transaction not found", false);

                decimal fee = balanceTransaction.Fee / 100M;

                return new ServiceResponse<(decimal gross, decimal fee)>("Success", true, (gross, fee));
            }
            catch (Exception ex)
            {
                return new ServiceResponse<(decimal gross, decimal fee)>($"Error getting transaction details: {ex.Message}", false);
            }
        }


        public async Task<Session?> GetStripeSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return null;

            SessionService service = new SessionService();
            return await service.GetAsync(sessionId);
        }

        public async Task<ServiceResponse<Donation>> CreateDonationRecordFromPaymentSession(PaymentSession paymentSession, Session stripeSession, decimal fee)
        {
            decimal gross = paymentSession.DonationAmount ?? paymentSession.SubscriptionAmount ?? 0;

            Donation donation = new Donation
            {
                LocationId = paymentSession.LocationId.Value,
                DonationCampaignId = paymentSession.DonationCampaignId.Value,
                Email = paymentSession.Email,
                TransactionFee = fee,
                TransactionId = stripeSession.PaymentIntentId,
                FirstName = paymentSession.FirstName,
                LastName = paymentSession.LastName,
                DonationDate = DateTime.UtcNow,
                TaxFormSent = false,
                PaymentProcessor = "Stripe",
                PaymentStatus = stripeSession.PaymentStatus,
                Gross = gross,
                Currency = stripeSession.Currency?.ToUpper()
            };

            return await _donationDataService.CreateAsync(donation);
        }
    }
}
