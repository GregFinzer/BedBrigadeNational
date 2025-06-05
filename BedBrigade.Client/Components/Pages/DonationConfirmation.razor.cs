using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using System.Web;
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

    protected override async Task OnInitializedAsync()
    {
        _lc.InitLocalizedComponent(this);
        
        try
        {
            var uri = new Uri(_navigationManager.Uri);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var sessionId = query["sessionid"];

            // Get transaction details from Stripe
            var stripeSession = await _paymentService.GetStripeSession(sessionId);
            if (stripeSession == null)
            {
                //TODO:  Localize
                ErrorMessage = "Stripe Session Not Found";
                return;
            }

            var transactionDetails = await _paymentService.GetStripeTransactionDetails(stripeSession.Id);
            if (!transactionDetails.Success)
            {
                ErrorMessage = transactionDetails.Message;
                return;
            }

            // Calculate final amount
            Amount = transactionDetails.Data.gross - transactionDetails.Data.fee;
            TransactionId = stripeSession.PaymentIntentId;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
    }


}
