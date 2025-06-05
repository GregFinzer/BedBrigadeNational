using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using BedBrigade.Common.Enums;
using Microsoft.AspNetCore.Components.Forms;
using BedBrigade.SpeakIt;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Components.Pages;

public partial class Donations : ComponentBase, IDisposable
{
    [Inject] private ILocationDataService _svcLocation { get; set; }
    [Inject] private NavigationManager _nav { get; set; }
    [Inject] private ILanguageContainerService _lc { get; set; }
    [Inject] private IConfigurationDataService _configSvc { get; set; }
    [Inject] private ILanguageService _svcLanguage { get; set; }
    [Inject] private ITranslationDataService _translateLogic { get; set; }
    
    [Inject] private ICustomSessionService _customSessionService { get; set; }
    [Inject] private IDonationCampaignDataService _donationCampaignService { get; set; }
    [Inject] private IJSRuntime _js { get; set; }
    [Inject] private IPaymentService _paymentService { get; set; }

    [Parameter] public string LocationRoute { get; set; } = default!;
    public int? LocationId { get; set; }
    public string LocationName { get; set; }
    public List<decimal>? DonationAmounts { get; set; }
    public List<decimal>? SubscriptionAmounts { get; set; }
    public List<DonationCampaign>? DonationCampaigns { get; set; } 
    public string RotatorTitle { get; set; }
    private const string DonationName = "Donations";
    public PaymentSession? PaymentSession { get; set; }
    private EditContext? EC { get; set; }
    private const string DisplayNone = "none";
    private string MyValidationMessage = string.Empty;
    private string MyValidationDisplay = DisplayNone;
    private ReCAPTCHA? reCAPTCHAComponent;
    private bool ValidReCAPTCHA = false;
    private ValidationMessageStore _validationMessageStore;
    protected override async Task OnInitializedAsync()
    {
        _lc.InitLocalizedComponent(this);
        await LoadData();
        _svcLanguage.LanguageChanged += OnLanguageChanged;
    }

    private async Task LoadData()
    {
        ServiceResponse<Location>? locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");
        if (locationResponse != null && locationResponse.Success && locationResponse.Data != null)
        {
            LocationId = locationResponse.Data.LocationId;
            LocationName = locationResponse.Data.Name;
            RotatorTitle = $"{locationResponse.Data.Name} {DonationName}";

            DonationAmounts = await _configSvc.GetAmounts(ConfigSection.Payments, ConfigNames.StripeDonationAmounts,
                LocationId.Value);

            SubscriptionAmounts = await _configSvc.GetAmounts(ConfigSection.Payments, ConfigNames.StripeSubscriptionAmounts,
                LocationId.Value);

            var donationCampaignsResponse = await _donationCampaignService.GetAllForLocationAsync(LocationId.Value);

            if (donationCampaignsResponse.Success && donationCampaignsResponse.Data != null)
            {
                DonationCampaigns = donationCampaignsResponse.Data;
            }

            PaymentSession = new PaymentSession();
            PaymentSession.LocationId = LocationId.Value;

            if (DonationCampaigns != null && DonationCampaigns.Count == 1)
            {
                PaymentSession.DonationCampaignId = DonationCampaigns.First().DonationCampaignId;
            }

            EC = new EditContext(PaymentSession);
            _validationMessageStore = new ValidationMessageStore(EC);
        }
    }

    private async Task OnLanguageChanged(CultureInfo arg)
    {
        await TranslatePageTitle();
        StateHasChanged();
    }

    public void Dispose()
    {
        _svcLanguage.LanguageChanged -= OnLanguageChanged;
    }

    private async Task TranslatePageTitle()
    {
        RotatorTitle = $"{LocationName} {DonationName}";

        if (_svcLanguage.CurrentCulture.Name != Defaults.DefaultLanguage)
        {
            RotatorTitle = await _translateLogic.GetTranslation(RotatorTitle, _svcLanguage.CurrentCulture.Name);
        }
    }

    private async Task ShowValidationMessage(string message)
    {
        MyValidationMessage = message;
        MyValidationDisplay = "";
        await _js.InvokeVoidAsync("BedBrigadeUtil.ScrollToElementId", "myValidationMessage");
    }
    private async Task<bool> IsValid()
    {
        ClearValidationMessage();
        bool formIsValid = true;
        //TODO:  Add key for DonationFormNotCompleted
        string formNotCompleted = "Donation Form not completed";
        formIsValid = ValidationLocalization.ValidateModel(PaymentSession, _validationMessageStore, _lc);

        if (!formIsValid)
        {
            await ShowValidationMessage(formNotCompleted);
            return false;
        }

        bool isPhoneValid = Validation.IsValidPhoneNumber(PaymentSession.PhoneNumber);

        if (!isPhoneValid)
        {
            _validationMessageStore.Add(new FieldIdentifier(PaymentSession, nameof(PaymentSession.PhoneNumber)), _lc.Keys["ValidPhoneNumber"]);
            await ShowValidationMessage(formNotCompleted);
            return false;
        }

        var emailResult = Validation.IsValidEmail(PaymentSession.Email);
        if (!emailResult.IsValid)
        {
            _validationMessageStore.Add(new FieldIdentifier(PaymentSession, nameof(PaymentSession.Email)), emailResult.UserMessage);
            await ShowValidationMessage(_lc.Keys["ValidEmail"]);
            return false;
        }

        if (!PaymentSession.DonationAmount.HasValue && !PaymentSession.SubscriptionAmount.HasValue)
        {
            //TODO:  Add key for this message
            await ShowValidationMessage("Donation Amount is required");
            return false;
        }

        if (!ValidReCAPTCHA)
        {
            await ShowValidationMessage(_lc.Keys["PleaseCheckRecaptcha"]);
            return false;
        }

        return true;
    }

    private void ClearValidationMessage()
    {
        MyValidationMessage = string.Empty;
        MyValidationDisplay = DisplayNone;
        _validationMessageStore.Clear();
    }
    private async Task SubmitRequest()
    {
        bool isValid = await IsValid();

        if (isValid)
        {
            await _customSessionService.SetItemAsync(Defaults.PaymentSessionKey, PaymentSession);
            var urlResponse = await _paymentService.GetStripeDepositUrl();
            await _customSessionService.SetItemAsync(Defaults.PaymentSessionKey, PaymentSession);

            if (urlResponse.Success && !string.IsNullOrEmpty(urlResponse.Data))
            {
                _nav.NavigateTo(urlResponse.Data);
            }
            else
            {
                await ShowValidationMessage(urlResponse.Message);
            }
        }
    }
    #region reCaptcha


    private void OnSuccess()
    {
        ValidReCAPTCHA = true;
    } // reCaptcha success

    private void OnExpired()
    {
        ValidReCAPTCHA = false;
    }

    #endregion
}