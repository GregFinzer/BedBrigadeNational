using BedBrigade.Client.Services;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.Inputs;
using ValidationLocalization = BedBrigade.SpeakIt.ValidationLocalization;

namespace BedBrigade.Client.Components.Pages
{
    public partial class ContactUs :ComponentBase
    {
        #region Declaration
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IContactUsDataService? _svcContactUs { get; set; }
        [Inject] private NavigationManager? _nav { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private IEmailBuilderService _svcEmailBuilder { get; set; }
        [Inject] private ILocationState _locationState { get; set; }
        [Parameter] public string? LocationRoute { get; set; }

        private Common.Models.ContactUs? newRequest;
        private SearchLocation? SearchLocation;

        private const string DisplayNone = "none";
        private const string AlertDanger = "alert alert-danger";

        private string DisplayForm = DisplayNone;
        private string DisplaySearch = DisplayNone;
        private string ResultMessage = string.Empty;

        private string ResultDisplay = DisplayNone;
        private string AlertType = AlertDanger;
        private bool ValidReCAPTCHA = false;

        private EditContext? EC { get; set; }

        private string MyValidationMessage = string.Empty;
        private string MyValidationDisplay = DisplayNone;
        

        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "4" },
        };

        public required SfMaskedTextBox phoneTextBox;

        private ValidationMessageStore _validationMessageStore;
        #endregion

        #region Initialization

        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
            
            newRequest = new Common.Models.ContactUs();
            EC = new EditContext(newRequest);
            _validationMessageStore = new ValidationMessageStore(EC);
            await SetLocationState();
        }

        private async Task SetLocationState()
        {
            if (!string.IsNullOrEmpty(LocationRoute))
            {
                if (await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute.ToLower()}") is { Success: true, Data: { } location })
                {
                    _locationState.Location = LocationRoute;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!string.IsNullOrEmpty(LocationRoute))
                {
                    await SearchLocation.ForceLocationByName(LocationRoute);
                    DisplayForm = "";
                    StateHasChanged();
                }
                else
                {
                    DisplaySearch = "";
                    StateHasChanged();
                }
            }
        }

        #endregion

        #region Validation & Events

        private void ClearValidationMessage()
        {
            MyValidationMessage = string.Empty;
            MyValidationDisplay = DisplayNone;
            _validationMessageStore.Clear();
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
            bool isValid = true;
            
            
            isValid = ValidationLocalization.ValidateModel(newRequest, _validationMessageStore, _lc);

            if (!isValid)
            {
                EC.NotifyValidationStateChanged();
                await ShowValidationMessage(_lc.Keys["ContactUsFormNotCompleted"]);
                return false;
            }

            bool isPhoneValid = Validation.IsValidPhoneNumber(newRequest.Phone);

            if (!isPhoneValid)
            {
                _validationMessageStore.Add(new FieldIdentifier(newRequest, nameof(newRequest.Phone)), _lc.Keys["ValidPhoneNumber"]);
                await ShowValidationMessage(_lc.Keys["ContactUsFormNotCompleted"]);
                return false;
            }

            var emailResult = Validation.IsValidEmail(newRequest.Email);
            if (!emailResult.IsValid)
            {
                _validationMessageStore.Add(new FieldIdentifier(newRequest, nameof(newRequest.Email)), emailResult.UserMessage);
                await ShowValidationMessage(_lc.Keys["ValidEmail"]);
                return false;
            }

            if (!ValidReCAPTCHA)
            {
                await ShowValidationMessage(_lc.Keys["PleaseCheckRecaptcha"]);
                return false;
            }

            return true;
        } // Run Validation





        private void CheckChildData(string SearchZipCode)  // from Search Location Component
        {
            DisplayForm = "";
        }

        #endregion

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

        #region SaveRequest

        private async Task SaveRequest()
        {
            if (!await IsValid())
                return;

            newRequest.LocationId = SearchLocation.ddlValue; // get value from child component
            newRequest.Phone = newRequest.Phone.FormatPhoneNumber();
            newRequest.Status = ContactUsStatus.ContactRequested;
            await UpdateDatabase();
            await SendConfirmationEmail(newRequest);
        }

        private async Task SendConfirmationEmail(Common.Models.ContactUs contactUs)
        {
            var emailResult = await _svcEmailBuilder.SendContactUsConfirmationEmail(contactUs);

            if (!emailResult.Success)
            {
                AlertType = AlertDanger;
                ResultMessage = emailResult.Message;
                ResultDisplay = "";
                await ScrollToResultMessage();
            }
        }

        private async Task UpdateDatabase()
        {
            try
            {

                var addResult = await _svcContactUs.CreateAsync(newRequest);
                if (addResult.Success && addResult.Data != null)
                {
                    newRequest = addResult.Data; // added Request
                }

                if (newRequest != null && newRequest.ContactUsId > 0)
                {
                    AlertType = "alert alert-success";
                    DisplaySearch = DisplayNone;
                    DisplayForm = DisplayNone;
                    ResultMessage = _lc.Keys["ContactUsFormSubmitted"];
                    ResultDisplay = "";
                }
                else
                {
                    ResultMessage = addResult.Message;
                    AlertType = AlertDanger;
                    ResultDisplay = "";
                    Log.Error("Error saving ContactUs: " + addResult.Message);
                }
            }
            catch (Exception ex)
            {
                AlertType = AlertDanger;
                ResultMessage = "Error! " + ex.Message;
                ResultDisplay = "";
                Log.Error(ex, "Error saving ContactUs");
            }
            finally
            {
                await ScrollToResultMessage();
            }
        }

        private async Task ScrollToResultMessage()
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.ScrollToElementId", "resultMessage", 200);
        }
        // update database

        #endregion
        public async Task HandlePhoneMaskFocus()
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
        }

    }
}
