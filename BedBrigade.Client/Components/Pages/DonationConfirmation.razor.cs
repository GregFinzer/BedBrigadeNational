using System.Collections.Specialized;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using System.Web;
using Serilog;
using Stripe.Checkout;

namespace BedBrigade.Client.Components.Pages;

public partial class DonationConfirmation : ComponentBase
{
    [Inject] private ICustomSessionService _customSessionService { get; set; }
    [Inject] private ILanguageContainerService _lc { get; set; }
    [Inject] private IPaymentService _paymentService { get; set; }
    [Inject] private IDonationDataService _donationDataService { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }

    protected string? ErrorMessage { get; set; }
    protected decimal? Amount { get; set; }
    protected string? TransactionId { get; set; }
    protected string? EncryptedSessionId { get; set; }

    protected override void OnInitialized()
    {
        _lc.InitLocalizedComponent(this);

        try
        {
            Uri uri = new Uri(_navigationManager.Uri);
            NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
            EncryptedSessionId = query["sessionid"];
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || string.IsNullOrEmpty(EncryptedSessionId))
            return;

        try
        {
            // Get payment session from local storage
            PaymentSession? paymentSession = await _customSessionService.GetItemAsync<PaymentSession>(Defaults.PaymentSessionKey);
            if (paymentSession == null)
            {
                ErrorMessage = "Payment Session Not Found";
                return;
            }

            if (!await _paymentService.VerifySessionId(paymentSession, EncryptedSessionId))
            {
                ErrorMessage = "Invalid Session";
                return;
            }


            Session? stripeSession = await _paymentService.GetStripeSession(paymentSession.StripeSessionId);
            if (stripeSession == null)
            {
                ErrorMessage = "Stripe session not found";
                return;
            }

            ServiceResponse<(decimal gross, decimal fee)> transactionDetails = await _paymentService.GetStripeTransactionDetails(stripeSession.Id);
            Amount = transactionDetails.Data.gross;
            TransactionId = stripeSession.PaymentIntentId;
            var createRecordResult =
                await _paymentService.CreateDonationRecordFromPaymentSession(paymentSession, stripeSession,
                    transactionDetails.Data.fee);

            if (!createRecordResult.Success)
            {
                ErrorMessage = createRecordResult.Message;
            }

            await _customSessionService.RemoveItemAsync(Defaults.PaymentSessionKey);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error processing donation: {ex.Message}";
            Log.Error(ex, "Error processing donation in DonationConfirmation component");
        }

    }


}
