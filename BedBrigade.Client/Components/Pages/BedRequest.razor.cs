using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Inputs;
using System.Globalization;
using static OfficeOpenXml.ExcelErrorValue;
using ValidationLocalization = BedBrigade.SpeakIt.ValidationLocalization;

namespace BedBrigade.Client.Components.Pages
{
    public partial class BedRequest : ComponentBase, IDisposable
    {
        #region Declaration

        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IBedRequestDataService? _svcBedRequest { get; set; }
        [Inject] private NavigationManager? _nav { get; set; }

        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private IEmailBuilderService _svcEmailBuilder { get; set; }
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private IGeoLocationQueueDataService? _svcGeoLocation { get; set; }
        [Inject] private ILanguageService _svcLanguage { get; set; }
        [Inject] private ITranslationDataService _translateLogic { get; set; }
        [Inject] private ILocationState _locationState { get; set; }

        private Common.Models.NewBedRequest? newRequest;
        private List<UsState>? StateList = AddressHelper.GetStateList();
        protected List<string>? lstPrimaryLanguage;
        protected List<DisplayTextValue>? lstSpeakEnglish;

        private SearchLocation? SearchLocation;

        private const string DisplayNone = "none";
        private const string AlertDanger = "alert alert-danger";

        private string DisplayForm = DisplayNone;
        private string DisplaySearch = DisplayNone;
        private string DisplaySpeakEnglish = DisplayNone;

        public int NumericValue { get; set; } = 1;

        private string ResultMessage = string.Empty;
        private string ResultDisplay = DisplayNone;

        private bool ValidReCAPTCHA = false;
        
        private string MyValidationMessage = string.Empty;
        private string MyValidationDisplay = DisplayNone;
        private EditContext? EC { get; set; }


        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "4" },
        };


        protected Dictionary<string, object> htmlattributeSize = new Dictionary<string, object>()
        {
           { "maxlength", "2" },
        };

        [Parameter] public string? LocationRoute { get; set; }
        private ValidationMessageStore _validationMessageStore;
        private string AlertType = AlertDanger;
        public required SfMaskedTextBox phoneTextBox;
        public required SfMaskedTextBox zipTextBox;
        protected bool _isBusy = false;
        #endregion
        #region Initialization

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                newRequest = new Common.Models.NewBedRequest();
                EC = new EditContext(newRequest);
                _validationMessageStore = new ValidationMessageStore(EC);
                await SetLocationState();
                await LoadConfiguration();
                _svcLanguage.LanguageChanged += OnLanguageChanged;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing BedRequest component");
                ResultMessage = "Error initializing BedRequest component";
                AlertType = AlertDanger;
                ResultDisplay = "";
            }
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

        private async Task OnLanguageChanged(CultureInfo arg)
        {
            await TranslateSpeakEnglish();
            StateHasChanged();
        }

        public void Dispose()
        {
            _svcLanguage.LanguageChanged -= OnLanguageChanged;
        }

        private async Task LoadConfiguration()
        {
            lstPrimaryLanguage = await _svcConfiguration.GetPrimaryLanguages();
            List<string> speakEnglish = await _svcConfiguration.GetSpeakEnglish();
            lstSpeakEnglish = ObjectUtil.ListStringToListDisplayTextValue(speakEnglish);
            await TranslateSpeakEnglish();
        } 

        private async Task TranslateSpeakEnglish()
        {
            foreach (var displayTextValue in lstSpeakEnglish)
            {
                if (_svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
                {
                    displayTextValue.DisplayText = displayTextValue.Value;
                }
                else
                {
                    displayTextValue.DisplayText = await _translateLogic.GetTranslation("Dynamic" + displayTextValue.Value, _lc.CurrentCulture.Name);

                    if (displayTextValue.DisplayText.Contains("Dynamic"))
                        displayTextValue.DisplayText = displayTextValue.Value; 
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

        private void CheckChildData(string SearchZipCode)  // from Search Location Component
        { // usually data is zip code
            if (SearchZipCode != null && SearchZipCode.Trim().Length == 5)
            {
                DisplayForm = "";
                newRequest.City = Validation.GetCityForZipCode(SearchZipCode);
                newRequest.State = Validation.GetStateForZipCode(SearchZipCode);
                newRequest.PostalCode = SearchZipCode;

            }
        } // check child component data              

        #endregion

        #region Validation & Events

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

            formIsValid = ValidationLocalization.ValidateModel(newRequest, _validationMessageStore, _lc);

            if (!formIsValid)
            {
                await ShowValidationMessage(_lc.Keys["BedRequestFormNotCompleted"]);
                return false;
            }

            if (!await DeepValidation()) return false;

            if (!ValidReCAPTCHA)
            {
                await ShowValidationMessage(_lc.Keys["PleaseCheckRecaptcha"]);
                return false;
            }

            return true;
        }

        private async Task<bool> DeepValidation()
        {
            newRequest.LocationId = SearchLocation.ddlValue;
            DateTime? nextEligibleDate = (await _svcBedRequest.NextDateEligibleForBedRequest(newRequest)).Data;

            if (nextEligibleDate.HasValue)
            {
                await ShowValidationMessage(_lc.Keys["RecentlyGivenBed"] + " " + nextEligibleDate.Value.ToShortDateString());
                return false;
            }

            if (newRequest.PrimaryLanguage != "English")
            {
                if (string.IsNullOrEmpty(newRequest.SpeakEnglish))
                {
                    _validationMessageStore.Add(new FieldIdentifier(newRequest, nameof(newRequest.SpeakEnglish)), _lc.Keys["RequiredSpeakEnglish"]);
                    await ShowValidationMessage(_lc.Keys["BedRequestFormNotCompleted"]);
                    return false;
                }
            }

            bool isPhoneValid = Validation.IsValidPhoneNumber(newRequest.Phone);

            if (!isPhoneValid)
            {
                _validationMessageStore.Add(new FieldIdentifier(newRequest, nameof(newRequest.Phone)), _lc.Keys["ValidPhoneNumber"]);
                await ShowValidationMessage(_lc.Keys["BedRequestFormNotCompleted"]);
                return false;
            }

            var emailResult = Validation.IsValidEmail(newRequest.Email);
            if (!emailResult.IsValid)
            {
                _validationMessageStore.Add(new FieldIdentifier(newRequest, nameof(newRequest.Email)), emailResult.UserMessage);
                await ShowValidationMessage(_lc.Keys["ValidEmail"]);
                return false;
            }

            string addressMessage = ValidateAddress();

            if (!string.IsNullOrEmpty(addressMessage))
            {
                await ShowValidationMessage(addressMessage);
                return false;
            }

            string distanceMessage = await ValidateZipDistance();

            if (!string.IsNullOrEmpty(distanceMessage))
            {
                await ShowValidationMessage(distanceMessage);
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

        private async Task<string> ValidateZipDistance()
        {
            var locationResponse = await _svcLocation.GetBedBrigadeNearMe(newRequest.PostalCode);

            if (locationResponse.Success && locationResponse.Data != null)
            {
                var locations = locationResponse.Data;
                if (locations.Count > 0)
                {
                    if (!locations.Any(o => o.LocationId == newRequest.LocationId))
                    {
                        newRequest.LocationId = locations[0].LocationId;
                    }

                    return string.Empty;
                }
                else
                {
                    return _lc.Keys["NoBedBrigadeNear"];
                }
            }
            else
            {
                return locationResponse.Message;
            }
        }

        private string ValidateAddress()
        {
            if (!Validation.IsValidZipCode(newRequest.PostalCode))
            {
                return _lc.Keys["InvalidPostalCode"];
            }

            List<string> cities = Validation.GetCitiesForZipCode(newRequest.PostalCode);

            if (!cities.Any(o => o.ToLower() == newRequest.City.ToLower()))
            {
                string cityNames = string.Join(", ", cities);
                return _lc.Keys["InvalidCity"] + " " + cityNames;
            }

            string stateForZipCode = Validation.GetStateForZipCode(newRequest.PostalCode);
            if (newRequest.State != stateForZipCode)
            {
                return _lc.Keys["StateNotMatchZipCode"] + " " + stateForZipCode;
            }

            return string.Empty;

        }
        public async Task OnPrimaryLanguageChange(ChangeEventArgs<string, string> args)
        {
            DisplaySpeakEnglish = DisplayNone;
            if (args.Value != null)
            {
                if (args.Value.ToString() != "English")
                {
                    DisplaySpeakEnglish = "";
                }
            }
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

        private Task RefreshPage()
        {
            _nav.NavigateTo(_nav.Uri, true);
            return Task.CompletedTask;
        }

        private async Task SaveRequest()
        {
            _isBusy = true;

            try
            {
                bool isValid = await IsValid();

                if (isValid)
                {
                    newRequest.LocationId = SearchLocation.ddlValue; // get value from child component
                    newRequest.NumberOfBeds = NumericValue;
                    newRequest.Phone = newRequest.Phone.FormatPhoneNumber();
                    newRequest.Group = (await _svcLocation.GetByIdAsync(newRequest.LocationId)).Data.Group;
                    newRequest.Contacted = false;
                    newRequest.BedType = Defaults.DefaultBedType;

                    if (newRequest.PrimaryLanguage == "English")
                    {
                        newRequest.SpeakEnglish = "Yes";
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(newRequest.SpeakEnglish))
                        {
                            newRequest.SpeakEnglish = "No"; // cannot be null
                        }
                    }

                    var bedRequest = await UpdateDatabase();

                    if (bedRequest != null)
                    {
                        await SendConfirmationEmail(bedRequest);
                    }
                }
            }
            finally
            {
                _isBusy = false;
            }
        }


        private async Task SendConfirmationEmail(Common.Models.BedRequest bedRequest)
        {
            var emailResult = await _svcEmailBuilder.SendBedRequestConfirmationEmail(bedRequest);

            if (!emailResult.Success)
            {
                AlertType = AlertDanger;
                ResultMessage = emailResult.Message;
                ResultDisplay = "";
                await ScrollToResultMessage();
            }
        }

        private async Task ScrollToResultMessage()
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.ScrollToElementId", "resultMessage", 100);
        }

        private async Task<Common.Models.BedRequest?> UpdateDatabase()
        {
            try
            {
                Common.Models.BedRequest? bedRequest = await BuildBedRequest();
                Common.Models.BedRequest? existingBedRequest = null;

                var existingByPhone = await _svcBedRequest.GetWaitingByPhone(bedRequest.Phone);

                if (existingByPhone.Success && existingByPhone.Data != null)
                {
                    existingBedRequest = existingByPhone.Data;
                }
                else
                {
                    var existingByEmail = await _svcBedRequest.GetWaitingByEmail(bedRequest.Email);
                    if (existingByEmail.Success && existingByEmail.Data != null)
                    {
                        existingBedRequest = existingByEmail.Data;
                    }
                }

                if (existingBedRequest != null)
                {
                    return await UpdateExistingBedRequest(existingBedRequest, bedRequest);
                }
                else
                {
                    return await CreateNewBedRequest(bedRequest);
                }
            }
            catch (Exception ex)
            {
                AlertType = AlertDanger;
                ResultMessage = "Error! " + ex.Message;
                ResultDisplay = "";
                Log.Error(ex, "Error saving BedRequest");
            }
            finally
            {
                await ScrollToResultMessage();
            }

            return null;
        }

        private async Task<Common.Models.BedRequest?> UpdateExistingBedRequest(Common.Models.BedRequest existingBedRequest, Common.Models.BedRequest bedRequest)
        {
            existingBedRequest.UpdateDuplicateFields(bedRequest, $"Updated on {DateTime.Now.ToShortDateString()} by requester.");

            var updateResult = await _svcBedRequest.UpdateAsync(existingBedRequest);
            if (updateResult.Success && updateResult.Data != null)
            {
                bedRequest = updateResult.Data;

                AlertType = "alert alert-success";
                DisplaySearch = DisplayNone;
                DisplayForm = DisplayNone;
                ResultMessage = _lc.Keys["BedRequestFormUpdated"];
                ResultDisplay = "";
                return bedRequest;
            }

            ResultMessage = updateResult.Message;
            AlertType = AlertDanger;
            ResultDisplay = "";
            Log.Error("Error updating BedRequest: " + updateResult.Message);
            return null;
        }

        private async Task<Common.Models.BedRequest?> CreateNewBedRequest(Common.Models.BedRequest bedRequest)
        {
            var addResult = await _svcBedRequest.CreateAsync(bedRequest);
            if (addResult.Success && addResult.Data != null)
            {
                bedRequest = addResult.Data;
                AlertType = "alert alert-success";
                DisplaySearch = DisplayNone;
                DisplayForm = DisplayNone;
                ResultMessage = _lc.Keys["BedRequestFormCreated"];
                ResultDisplay = "";
                return bedRequest;
            }

            ResultMessage = addResult.Message;
            AlertType = AlertDanger;
            ResultDisplay = "";
            Log.Error("Error creating BedRequest: " + addResult.Message);
            return null;
        }

        private async Task<Common.Models.BedRequest> BuildBedRequest()
        {
            //Set it to the primary city name
            newRequest.City = Validation.GetCityForZipCode(newRequest.PostalCode);
            newRequest.Reference = "National Website";
            Common.Models.BedRequest bedRequest = new Common.Models.BedRequest();
            ObjectUtil.CopyProperties(newRequest, bedRequest);
            string defaultNote = await _svcConfiguration.GetConfigValueAsync(ConfigSection.CustomStrings,
                ConfigNames.BedRequestNote, bedRequest.LocationId);

            if (String.IsNullOrWhiteSpace(newRequest.SpecialInstructions))
            {
                bedRequest.Notes = defaultNote;
            }
            else
            {
                bedRequest.Notes = defaultNote + " " + newRequest.SpecialInstructions;
            }

            return bedRequest;
        }

        #endregion
        public async Task HandlePhoneMaskFocus()
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
        }
        public async Task HandleZipMaskFocus()
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", zipTextBox.ID, 0);
        }
    }
}
