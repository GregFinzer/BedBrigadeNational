using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Logic;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;
using ValidationLocalization = BedBrigade.SpeakIt.ValidationLocalization;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Syncfusion.Blazor.DropDowns;
using System.Globalization;
using System;


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

        private Common.Models.NewBedRequest? newRequest;
        private List<UsState>? StateList = AddressHelper.GetStateList();
        protected List<string>? lstPrimaryLanguage;
        protected List<DisplayTextValue>? lstSpeakEnglish;

        private SearchLocation? SearchLocation;

        private const string DisplayNone = "none";
        private const string AlertDanger = "alert alert-danger";

        private string DisplayForm = DisplayNone;
        private string DisplayAddressMessage = DisplayNone;
        private string DisplaySearch = DisplayNone;
        private string DisplaySpeakEnglish = DisplayNone;

        public int NumericValue { get; set; } = 1;

        private string ResultMessage = string.Empty;
        private string ResultDisplay = DisplayNone;

        private ReCAPTCHA? reCAPTCHAComponent;
        private bool ValidReCAPTCHA = false;
        private bool ServerVerificatiing = false;
        
        private bool isAddressCorrect = false;
        private string? _locationQueryParm;

        private string MyValidationMessage = string.Empty;
        private string MyValidationDisplay = DisplayNone;
      

        private bool DisableSubmitButton => !ValidReCAPTCHA || ServerVerificatiing;
        private EditContext? EC { get; set; }


        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "4" },
        };

        protected Dictionary<string, object> DropDownHtmlAttribute = new Dictionary<string, object>()
        {
           { "font-weight", "bold" },
        };

        protected Dictionary<string, object> htmlattributeSize = new Dictionary<string, object>()
        {
           { "maxlength", "2" },
        };

        [Parameter] public string PreloadLocation { get; set; }
        private ValidationMessageStore _validationMessageStore;
        private string AlertType = AlertDanger;
        #endregion
        #region Initialization

        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
            newRequest = new Common.Models.NewBedRequest();
            EC = new EditContext(newRequest);
            _validationMessageStore = new ValidationMessageStore(EC);

            if (!string.IsNullOrEmpty(PreloadLocation))
            {
                _locationQueryParm = PreloadLocation;
            }
            else
            {
                var uri = _nav.ToAbsoluteUri(_nav.Uri);

                if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("location", out var locationQueryParm))
                {
                    _locationQueryParm = locationQueryParm;
                }
            }
            await LoadConfiguration();
            _svcLanguage.LanguageChanged += OnLanguageChanged;
            base.OnInitialized();
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
            var dataConfiguration = await _svcConfiguration.GetAllAsync(ConfigSection.System); // Configuration ============================
            if (dataConfiguration.Success && dataConfiguration != null)
            {
                var dctConfiguration = dataConfiguration.Data.ToDictionary(keySelector: x => x.ConfigurationKey, elementSelector: x => x.ConfigurationValue);
                string delimitedString = dctConfiguration[ConfigNames.PrimaryLanguage].ToString();
                lstPrimaryLanguage = new List<string>(delimitedString.Split(';'));
                delimitedString = dctConfiguration[ConfigNames.SpeakEnglish].ToString();
                string[] values = delimitedString.Split(";", StringSplitOptions.RemoveEmptyEntries);
                lstSpeakEnglish = new List<DisplayTextValue>();
                foreach (var value in values)
                {
                    lstSpeakEnglish.Add(new DisplayTextValue
                    {
                        Value = value,
                        DisplayText = value 
                    });
                }
                await TranslateSpeakEnglish();
            }
        } // Configuration

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
                    displayTextValue.DisplayText = await _translateLogic.GetTranslation(displayTextValue.Value, _lc.CurrentCulture.Name);
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!string.IsNullOrEmpty(_locationQueryParm))
                {
                    await SearchLocation.ForceLocationByName(_locationQueryParm);
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

            if (!ValidReCAPTCHA)
            {
                await ShowValidationMessage(_lc.Keys["PleaseCheckRecaptcha"]);
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
                return _lc.Keys["InvalidCity", new {cityNames = cityNames}];
            }

            string stateForZipCode = Validation.GetStateForZipCode(newRequest.PostalCode);
            if (newRequest.State != stateForZipCode)
            {
                return _lc.Keys["StateNotMatchZipCode", stateForZipCode];
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
            bool isValid = await IsValid();

            if (isValid)
            {
                newRequest.LocationId = SearchLocation.ddlValue; // get value from child component
                newRequest.NumberOfBeds = NumericValue;
                newRequest.Phone = newRequest.Phone.FormatPhoneNumber();
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
                    await QueueForGeoLocation(bedRequest);
                }
            }
        }

        private async Task QueueForGeoLocation(Common.Models.BedRequest bedRequest)
        {
            GeoLocationQueue item = new GeoLocationQueue();
            item.Street = bedRequest.Street;
            item.City = bedRequest.City;
            item.State = bedRequest.State;
            item.PostalCode = bedRequest.PostalCode;
            item.CountryCode = Defaults.CountryCode;
            item.TableName = TableNames.BedRequests.ToString();
            item.TableId = bedRequest.BedRequestId;
            item.QueueDate = DateTime.UtcNow;
            item.Priority = 1;
            item.Status = GeoLocationStatus.Queued.ToString();
            var result = await _svcGeoLocation.CreateAsync(item);

            if (!result.Success)
            {
                AlertType = AlertDanger;
                ResultMessage = result.Message;
                ResultDisplay = "";
                await ScrollToResultMessage();
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
                //Set it to the primary city name
                newRequest.City = Validation.GetCityForZipCode(newRequest.PostalCode);

                Common.Models.BedRequest bedRequest = new Common.Models.BedRequest();
                ObjectUtil.CopyProperties(newRequest, bedRequest);
                var addResult = await _svcBedRequest.CreateAsync(bedRequest);
                if (addResult.Success && addResult.Data != null)
                {
                    bedRequest = addResult.Data; // added Request
                }

                if (bedRequest != null && bedRequest.BedRequestId > 0)
                {
                    AlertType = "alert alert-success";
                    DisplaySearch = DisplayNone;
                    DisplayForm = DisplayNone;
                    ResultMessage = _lc.Keys["BedRequestFormSubmitted"];
                    ResultDisplay = "";
                    return bedRequest;
                }
                else
                {
                    ResultMessage = addResult.Message;
                    AlertType = AlertDanger;
                    ResultDisplay = "";
                    Log.Error("Error saving BedRequest: " + addResult.Message);
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

        #endregion

    }
}
