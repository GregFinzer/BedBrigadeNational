using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using BedBrigade.SpeakIt;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.Inputs;
using System.Globalization;
using ValidationLocalization = BedBrigade.SpeakIt.ValidationLocalization;

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
    [Inject] private IContentDataService _contentDataService { get; set; }
    [Inject] private ILoadImagesService _loadImagesService { get; set; }
    [Inject] private ICarouselService _carouselService { get; set; }
    [Inject] private IContentTranslationDataService _svcContentTranslation { get; set; }
    [Inject] private ILocationState _locationState { get; set; }
    [Inject] private IIFrameControlService _iFrameControlService { get; set; }
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

    private bool ValidReCAPTCHA = false;
    private ValidationMessageStore _validationMessageStore;
    public string BodyContent { get; set; } = string.Empty;
    public string PreviousBodyContent { get; set; } = string.Empty;
    public required SfMaskedTextBox phoneTextBox;
    protected override async Task OnInitializedAsync()
    {
        _lc.InitLocalizedComponent(this);
        await LoadData();
        _locationState.Location = LocationRoute;
        _svcLanguage.LanguageChanged += OnLanguageChanged;
    }

    private async Task LoadData()
    {
        ServiceResponse<Location>? locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");
        if (locationResponse.Success && locationResponse.Data != null)
        {
            LocationId = locationResponse.Data.LocationId;
            LocationName = locationResponse.Data.Name;
            RotatorTitle = $"{locationResponse.Data.Name} {DonationName}";

            await LoadBodyContent();
            DonationAmounts = await _configSvc.GetAmounts(ConfigSection.Payments, ConfigNames.StripeLocationDonationAmounts,
                LocationId.Value);

            SubscriptionAmounts = await _configSvc.GetAmounts(ConfigSection.Payments, ConfigNames.StripeLocationSubscriptionAmounts,
                LocationId.Value);

            var donationCampaignsResponse = await _donationCampaignService.GetAllForLocationAsync(LocationId.Value);

            if (donationCampaignsResponse.Success && donationCampaignsResponse.Data != null)
            {
                DonationCampaigns = donationCampaignsResponse.Data;
            }
            else
            {
                Log.Error($"Donations, No Donation Campaigns Defined for {LocationRoute}");
                await ShowValidationMessage($"Donations, No Donation Campaigns Defined for {LocationRoute}");
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
        else
        {
            Log.Error($"Donations, Could not load location {LocationRoute}: " + locationResponse.Message);
            await ShowValidationMessage($"Could not load location {LocationRoute}:" + locationResponse.Message);
        }
    }

    private async Task LoadBodyContent()
    {
        if (_svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
        {
            await LoadDefaultContent();
        }
        else
        {
            await LoadContentByLanguage();
        }
    }

    private async Task LoadDefaultContent()
    {
        var contentResult = await _contentDataService.GetAsync("Donations", LocationId.Value);
        if (contentResult.Success)
        {
            var path = $"/{LocationRoute}/pages/Donations";
            string html = await ReplaceHtmlControls(path, contentResult.Data.ContentHtml);

            if (html != PreviousBodyContent)
            {
                PreviousBodyContent = html;
                BodyContent = html;
            }
        }
        else
        {
            PreviousBodyContent = string.Empty;
            BodyContent = string.Empty;
        }
    }

    private async Task LoadContentByLanguage()
    {
        var contentResult = await _svcContentTranslation.GetAsync("Donations", LocationId.Value, _svcLanguage.CurrentCulture.Name);
        if (contentResult.Success)
        {
            var path = $"/{LocationRoute}/pages/Donations";
            string html = await ReplaceHtmlControls(path, contentResult.Data.ContentHtml);

            if (html != PreviousBodyContent)
            {
                PreviousBodyContent = html;
                BodyContent = html;
            }

            return;
        }

        await LoadDefaultContent();
    }

    private async Task<string> ReplaceHtmlControls(string path, string html)
    {
        html = _loadImagesService.SetImagesForHtml(path, html);
        html = _carouselService.ReplaceCarousel(html);
        html = _iFrameControlService.ReplaceiFrames(html);
        return html;
    }

    private async Task OnLanguageChanged(CultureInfo arg)
    {
        await TranslatePageTitle();
        await LoadBodyContent();
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
        string formNotCompleted = _lc.Keys["DonationFormNotCompleted"];
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
            await ShowValidationMessage(_lc.Keys["RequiredDonationAmount"]);
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
            var urlResponse = await _paymentService.GetStripeDepositUrl(PaymentSession);

            if (urlResponse.Success && !string.IsNullOrEmpty(urlResponse.Data))
            {
                await _customSessionService.SetItemAsync(Defaults.PaymentSessionKey, PaymentSession);
                _nav.NavigateTo(urlResponse.Data);
            }
            else
            {
                Log.Error($"Donations, Error getting Stripe URL: {urlResponse.Message}");
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

    public async Task HandlePhoneMaskFocus()
    {
        await _js.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
    }

}