using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Logic;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Diagnostics;
using AKSoftware.Localization.MultiLanguages;
using AKSoftware.Localization.MultiLanguages.Blazor;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;


namespace BedBrigade.Client.Components.Pages
{
    public partial class BedRequest : ComponentBase
    {
        #region Declaration

        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IBedRequestDataService? _svcBedRequest { get; set; }
        [Inject] private NavigationManager? _nav { get; set; }

        [Inject] private ILanguageContainerService _lc { get; set; }

        private Common.Models.BedRequest? newRequest;
        private List<UsState>? StateList = AddressHelper.GetStateList();

        private SearchLocation? SearchLocation;

        private const string DisplayNone = "none";
        private const string AlertDanger = "alert alert-danger";
        private const string FormMessage = "Please fill out all the mandatory fields marked with an asterisk (*).";
        private const string FormNotCompleted = "The Bed Request Form is not completed!";

        private string DisplayForm = DisplayNone;
        private string DisplayAddressMessage = DisplayNone;
        private string DisplaySearch = DisplayNone;
        public int NumericValue { get; set; } = 1;

        private string SuccessClass = "alert alert-success";
        private string SuccessMessage = string.Empty;
        private string SuccessDisplay = DisplayNone;

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

        #endregion
        #region Initialization

        protected override void OnInitialized()
        {
            _lc.InitLocalizedComponent(this);
            newRequest = new Common.Models.BedRequest();
            EC = new EditContext(newRequest);

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

            base.OnInitialized();
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

        private void ShowValidationMessage(string message)
        {
            MyValidationMessage = message;
            MyValidationDisplay = "";
        }
        private async Task<bool> IsValid()
        {
            ClearValidationMessage();
            bool formIsValid = EC.Validate();

            if (!formIsValid)
            {
                ShowValidationMessage(FormNotCompleted);
                return false;
            }

            bool isPhoneValid = Validation.IsValidPhoneNumber(newRequest.Phone);

            if (!isPhoneValid)
            {
                ShowValidationMessage("Phone numbers must be 10 digits with a valid area code and prefix.");
                return false;
            }

            var emailResult = Validation.IsValidEmail(newRequest.Email);
            if (!emailResult.IsValid)
            {
                ShowValidationMessage(emailResult.UserMessage);
                return false;
            }

            string addressMessage = ValidateAddress();

            if (!string.IsNullOrEmpty(addressMessage))
            {
                ShowValidationMessage(addressMessage);
                return false;
            }

            if (!ValidReCAPTCHA)
            {
                ShowValidationMessage("Please check reCAPTCHA");
                return false;
            }

            string distanceMessage = await ValidateZipDistance();

            if (!string.IsNullOrEmpty(distanceMessage))
            {
                ShowValidationMessage(distanceMessage);
                return false;
            }

            return true;
        } 

        private void ClearValidationMessage()
        {
            MyValidationMessage = string.Empty;
            MyValidationDisplay = DisplayNone;
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
                    return "No Bed Brigade Near That Zip Code";
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
                return "The zip code that you have entered is not a valid U.S. Zip Code";
            }

            List<string> cities = Validation.GetCitiesForZipCode(newRequest.PostalCode);

            if (!cities.Any(o => o.ToLower() == newRequest.City.ToLower()))
            {
                return "The city that you have entered does not match the zip code. Valid city names are: "
                    + String.Join(", ", cities);
            }

            string stateForZipCode = Validation.GetStateForZipCode(newRequest.PostalCode);
            if (newRequest.State != stateForZipCode)
            {
                return "The state that you have entered does not match the zip code.  It should be " + stateForZipCode;
            }

            return string.Empty;

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
            var FormStatusMessage = "The Request Form is completed.";
            bool isValid = await IsValid();

            if (isValid)
            {
                newRequest.LocationId = SearchLocation.ddlValue; // get value from child component
                newRequest.NumberOfBeds = NumericValue;
                newRequest.Phone = newRequest.Phone.FormatPhoneNumber();

                await UpdateDatabase();
            }
        } 

        private async Task UpdateDatabase()
        {
            try
            {  
                //Set it to the primary city name
                newRequest.City = Validation.GetCityForZipCode(newRequest.PostalCode);
                
                var addResult = await _svcBedRequest.CreateAsync(newRequest);
                if (addResult.Success && addResult.Data != null)
                {
                    newRequest = addResult.Data; // added Request
                }

                if (newRequest != null && newRequest.BedRequestId > 0)
                {
                    //AlertType = "alert alert-success";
                    DisplaySearch = DisplayNone;
                    DisplayForm = DisplayNone;
                    // ResultMessage = "New Bed Request #" + newRequest.BedRequestId.ToString() + " created Successfully!<br />";
                    SuccessMessage += "We have received your request and would like to thank you for writing to us.<br />";
                    SuccessMessage += "We will look over your request and reply by email as soon as possible.<br />";
                    SuccessMessage += "Talk to you soon, Bed Brigade.";
                    SuccessDisplay = "";
                }
                else
                {
                    //SubmitAlertMessage = "Warning! Unable to add new Bed Request!";
                    //AlertType = AlertDanger;
                    //AlertDisplay = "";
                }
            }
            catch (Exception ex)
            {
                //AlertType = AlertDanger;
                //SubmitAlertMessage = "Error! " + ex.Message;
                //AlertDisplay = "";
            }
        } // update database

        #endregion

    }
}
