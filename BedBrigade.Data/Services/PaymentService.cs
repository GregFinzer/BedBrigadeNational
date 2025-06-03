using System;
using System.Collections.Generic;
using System.Web;
using System.Threading.Tasks;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
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

        public PaymentService(
            IConfigurationDataService configurationDataService,
            ICustomSessionService customSessionService,
            NavigationManager navigationManager,
            ILocationDataService locationDataService)
        {
            _configurationDataService = configurationDataService;
            _customSessionService = customSessionService;
            _navigationManager = navigationManager;
            _locationDataService = locationDataService;
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

        public async Task<ServiceResponse<string>> GetStripeDepositUrl()
        {
            const string donationConfirmationPage = "donation-confirmation";
            const string donationCancellationPage = "donation-cancellation";

            // Retrieve and validate Stripe secret key
            ServiceResponse<string> secretKeyResponse = await GetStripeSecretKeyAsync();
            if (!secretKeyResponse.Success || secretKeyResponse.Data == null)
                return new ServiceResponse<string>(secretKeyResponse.Message, false);
            StripeConfiguration.ApiKey = secretKeyResponse.Data!;

            // Retrieve and validate PaymentSession
            ServiceResponse<PaymentSession> sessionResponse = await GetAndValidatePaymentSessionAsync();
            if (!sessionResponse.Success || sessionResponse.Data == null )
                return new ServiceResponse<string>(sessionResponse.Message, false);
            PaymentSession paymentSession = sessionResponse.Data!;

            // Retrieve and validate Location
            ServiceResponse<Location> locationResponse = await _locationDataService
                .GetByIdAsync(paymentSession.LocationId.Value);
            if (!locationResponse.Success || locationResponse.Data == null)
                return new ServiceResponse<string>(
                    $"Location not found for ID {paymentSession.LocationId}", false);

            // Build RequestOptions (StripeAccount per location)
            RequestOptions requestOptions = await BuildRequestOptionsAsync(paymentSession.LocationId.Value);

            // Build line items (one–time vs. subscription)
            List<SessionLineItemOptions> lineItems = BuildLineItems(paymentSession);

            // Create encrypted session ID
            string sessionId = await CreateSessionId(paymentSession);

            // Build SessionCreateOptions
            var options = BuildSessionCreateOptions(
                paymentSession,
                lineItems,
                sessionId,
                donationConfirmationPage,
                donationCancellationPage);

            // Call Stripe to create the Checkout Session
            var service = new SessionService();
            var session = await service.CreateAsync(options, requestOptions);

            return new ServiceResponse<string>(session.Url, true);
        }

        private async Task<ServiceResponse<string>> GetStripeSecretKeyAsync()
        {
            var key = await _configurationDataService
                .GetConfigValueAsync(ConfigSection.Payments, ConfigNames.StripeSecretKey);
            if (string.IsNullOrEmpty(key))
                return new ServiceResponse<string>("Stripe secret key is not configured.", false);
            return new ServiceResponse<string>(key, true);
        }

        private async Task<ServiceResponse<PaymentSession>> GetAndValidatePaymentSessionAsync()
        {
            var paymentSession = await _customSessionService
                .GetItemAsync<PaymentSession>(Defaults.PaymentSessionKey);
            if (paymentSession == null)
                return new ServiceResponse<PaymentSession>("Payment session is not initialized.", false);

            var validationResult = ValidatePaymentSession(paymentSession);
            if (!validationResult.Success)
                return new ServiceResponse<PaymentSession>(validationResult.Message, false);

            return new ServiceResponse<PaymentSession>("Valid", true, paymentSession);
        }

        private async Task<RequestOptions> BuildRequestOptionsAsync(int locationId)
        {
            var accountId = await _configurationDataService
                .GetConfigValueAsync(ConfigSection.Payments, ConfigNames.StripeAccountId, locationId);
            return new RequestOptions { StripeAccount = accountId };
        }

        private List<SessionLineItemOptions> BuildLineItems(PaymentSession paymentSession)
        {
            var items = new List<SessionLineItemOptions>();

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
            var successUrl = new UriBuilder($"{GetBaseDomain()}/{successPage}")
            {
                Query = $"sessionid={HttpUtility.UrlEncode(sessionId)}"
            }.ToString();

            return new SessionCreateOptions
            {
                CustomerEmail = paymentSession.Email,
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

        private ServiceResponse<bool> ValidatePaymentSession(PaymentSession paymentSession)
        {
            if (!paymentSession.LocationId.HasValue)
                return new ServiceResponse<bool>("Location ID is required.", false);

            if (string.IsNullOrEmpty(paymentSession.FirstName)
                || string.IsNullOrEmpty(paymentSession.LastName))
                return new ServiceResponse<bool>("First name and last name are required.", false);

            var emailCheck = Validation.IsValidEmail(paymentSession.Email);
            if (!emailCheck.IsValid)
                return new ServiceResponse<bool>(emailCheck.UserMessage, false);

            if (string.IsNullOrEmpty(paymentSession.PhoneNumber)
                || !Validation.IsValidPhoneNumber(paymentSession.PhoneNumber))
                return new ServiceResponse<bool>("A valid phone number is required.", false);

            if (!paymentSession.DonationAmount.HasValue
                && !paymentSession.SubscriptionAmount.HasValue)
                return new ServiceResponse<bool>(
                    "Donation amount or subscription amount must be greater than zero.", false);

            if (paymentSession.DonationAmount <= 0
                && paymentSession.SubscriptionAmount <= 0)
                return new ServiceResponse<bool>(
                    "Donation amount or subscription amount must be greater than zero.", false);

            return new ServiceResponse<bool>(string.Empty, true);
        }
    }
}
