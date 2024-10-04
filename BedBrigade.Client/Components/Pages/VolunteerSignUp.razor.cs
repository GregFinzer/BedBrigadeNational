using Syncfusion.Blazor.DropDowns;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Forms;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Enums;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;
using Microsoft.AspNetCore.Mvc;
using Azure;
using BedBrigade.Common.Models;
using System.Net;
using BedBrigade.SpeakIt;
using ValidationLocalization = BedBrigade.SpeakIt.ValidationLocalization;

namespace BedBrigade.Client.Components.Pages
{
    public partial class VolunteerSignUp : ComponentBase
    {
        #region Declaration

        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IVolunteerDataService? _svcVolunteer { get; set; }
        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject] private ISignUpDataService? _svcSignUp { get; set; }
        [Inject] private NavigationManager? _nav { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ITranslateLogic _translateLogic { get; set; }
        private Volunteer? newVolunteer;
        private List<Schedule> LocationEvents { get; set; } = new List<Schedule>(); // Selected Location Events
        private Schedule? SelectedEvent { get; set; } // selected Event

        private List<SignUp> VolunteerRegister { get; set; } = new List<SignUp>(); // Volunteer/Events Registration

        private SearchLocation? SearchLocation;

        private const string DisplayNone = "none";

        private string DisplayForm = DisplayNone;
        private string DisplayAddressMessage = DisplayNone;
        private string DisplaySearch = DisplayNone;
        private string DisplayExId = DisplayNone;
        private string DisplayEventDetail = DisplayNone;
        private string DisplayLocationEvents = DisplayNone;
        private string DisplayLocationStatusMessage = DisplayNone;
        private string DisplayExistingMessage = DisplayNone;
        private string DisplayEmailMessage = DisplayNone;
        private MarkupString ExistingMessage = BootstrapHelper.GetBootstrapMessage("info", "Please enter your email address and we will check your data in our Database.", "", false, "compact");
        private MarkupString LocationEventsAlert = BootstrapHelper.GetBootstrapMessage("warning", "Sorry, there are no available volunteer events in the selected location.<br/>Please select another location or try again later.", "", false);


        private MarkupString FinalMessage;
        private string SubmitAlertMessage = string.Empty;
        private string ResultDisplay = DisplayNone;
        

        private ReCAPTCHA? reCAPTCHAComponent;
        private bool ValidReCAPTCHA = false;
        private bool ServerVerificatiing = false;
        private bool EditFormStatus = false; // true if not errors
        private int selectedLocation = 0;
        private bool isNewVolunteer = false;

        private bool DisableSubmitButton => !ValidReCAPTCHA || ServerVerificatiing;
        private EditContext? EC { get; set; }

        private string? _locationQueryParm;
        private string MyMessage = string.Empty;
        private string MyMessageDisplay = DisplayNone;
        private bool AreSignUpsAvailable = false;
        private SfDropDownList<int, Schedule> ddlSchedule;

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

        #endregion
        #region Initialization

        protected override void OnInitialized()
        {
            _lc.InitLocalizedComponent(this);
            newVolunteer = new Volunteer();
            EC = new EditContext(newVolunteer);
            _validationMessageStore = new ValidationMessageStore(EC);
            //Yes, this has to be here instead of in OnInitializedAsync
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
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!string.IsNullOrEmpty(_locationQueryParm))
                {
                    await SearchLocation.ForceLocationByName(_locationQueryParm);
                    await HandleSelectedValueChanged(SearchLocation.ddlValue.ToString());
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

        private void CheckChildData(string? searchZipCode)  // from Search Location Component
        { // usually data is zip code
            if (searchZipCode != null && searchZipCode.Trim().Length == 5)
            {
                DisplayForm = "";
                HandleSelectedValueChanged(SearchLocation.ddlValue.ToString());
            }
        } // check child component data


        #endregion

        #region Validation & Events


        private async Task HandleSelectedValueChanged(string locationIdString)
        {
            selectedLocation = Convert.ToInt32(locationIdString);
            await GetLocationEvents();
            StateHasChanged();
        }

        private void onPreviousVolunteer(Microsoft.AspNetCore.Components.ChangeEventArgs args)
        {
            DisplayExistingMessage = DisplayNone;
            if (newVolunteer.IHaveVolunteeredBefore)
            {
                DisplayExistingMessage = "";
            }
        }

        private async Task GetLocationEvents()
        { 
            try
            {
                ddlSchedule.Value = -1;
                DisplayLocationEvents = DisplayNone;
                DisplayLocationStatusMessage = DisplayNone;
                SelectedEvent = null;
                DisplayEventDetail = DisplayNone;

                var response = await _svcSchedule.GetAvailableSchedulesByLocationId(selectedLocation);

                if (response.Success && response.Data != null)
                {
                    LocationEvents = response.Data;
                }
                else
                {
                    Log.Logger.Error($"Error GetLocationEvents, response: {response.Message}");
                    ShowMessage(response.Message);
                }

                if (LocationEvents.Count > 0)
                {
                    DisplayLocationEvents = "";
                    AreSignUpsAvailable = true;
                }
                else
                {
                    DisplayLocationStatusMessage = "";
                    AreSignUpsAvailable = false;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error GetLocationEvents: {ex.Message}");
                ShowMessage(ex.Message);
            }

        }//GetLocationEvents

        private void ChangeEvent(ChangeEventArgs<int, Schedule> args)
        {
            if (args.Value > 0)
            {
                var intSchedulId = args.Value;
                SelectedEvent = LocationEvents.Where(item => item.ScheduleId == intSchedulId).FirstOrDefault();
                DisplayEventDetail = "";
            }
        }

        private void ClearMessage()
        {
            MyMessage = string.Empty;
            MyMessageDisplay = DisplayNone;
        }
        private void ShowMessage(string message)
        {
            MyMessage = message;
            MyMessageDisplay = "";
        }

        private bool IsValid()
        {
            ClearMessage();

            bool formIsValid = ValidationLocalization.ValidateModel(newVolunteer, _validationMessageStore, _lc);

            if (!formIsValid)
            {
                ShowMessage(_lc.Keys["VolunteerFormNotCompleted"]);
                return false;
            }

            bool isPhoneValid = Validation.IsValidPhoneNumber(newVolunteer.Phone);

            if (!isPhoneValid)
            {
                _validationMessageStore.Add(new FieldIdentifier(newVolunteer, nameof(newVolunteer.Phone)), _lc.Keys["ValidPhoneNumber"]);
                ShowMessage(_lc.Keys["VolunteerFormNotCompleted"]);
                return false;
            }

            var emailResult = Validation.IsValidEmail(newVolunteer.Email);
            if (!emailResult.IsValid)
            {
                _validationMessageStore.Add(new FieldIdentifier(newVolunteer, nameof(newVolunteer.Email)), emailResult.UserMessage);
                ShowMessage(_lc.Keys["ValidEmail"]);
                return false;
            }

            if (SelectedEvent == null)
            {
                ShowMessage(_lc.Keys["PleaseSelectAnEvent"]);
                return false;
            }

            if (!ValidReCAPTCHA)
            {
                ShowMessage(_lc.Keys["PleaseCheckRecaptcha"]);
                return false;
            }

            return true;
        } 









        #endregion    


        #region reCaptcha


        private void OnSuccess()
        {
            ValidReCAPTCHA = true;
        } 

        private void OnExpired()
        {
            ValidReCAPTCHA = false;
        }

        #endregion
        #region SaveVolunteer

        private Task RefreshPage()
        {
            _nav.NavigateTo(_nav.Uri, true);
            return Task.CompletedTask;
        }

        private async Task SaveVolunteer()
        {
            if (!IsValid())
            {
                return;
            }

            await UpdateDatabase();
        } 

        private async Task UpdateDatabase()
        {
            bool updateVolunteerSuccess = await UpdateVolunteer();

            if (!updateVolunteerSuccess)
                return;


            bool scheduleVolunteerSuccess= await ScheduleVolunteer();

            if (!scheduleVolunteerSuccess)
                return;

            bool updateCountSuccess = await UpdateScheduleVolunteerCount();

            if (!updateCountSuccess)
                return;

            await CreateFinalMessage();
        }

        private async Task<bool> ScheduleVolunteer()
        {
            try
            {

                SignUp newRegister = new SignUp();
                newRegister.ScheduleId = SelectedEvent.ScheduleId;
                newRegister.VolunteerId = newVolunteer.VolunteerId;
                newRegister.LocationId = selectedLocation;
                newRegister.SignUpNote = newVolunteer.Message;
                var createResult = await _svcSignUp.CreateAsync(newRegister);

                if (!createResult.Success)
                {
                    ShowMessage(createResult.Message);
                    Log.Logger.Error($"Error ScheduleVolunteer: {createResult.Message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error ScheduleVolunteer: {ex.Message}");
                ShowMessage(ex.Message);
                return false;
            }
        }

        private async Task<bool> UpdateVolunteer()
        {
            newVolunteer.LocationId = selectedLocation;

            try
            {
                var existingVolunteerResult = await _svcVolunteer.GetByEmail(newVolunteer.Email);

                if (existingVolunteerResult.Success && existingVolunteerResult.Data != null)
                {
                    var existingVolunteer = existingVolunteerResult.Data;
                    existingVolunteer.LocationId = newVolunteer.LocationId;
                    existingVolunteer.IHaveVolunteeredBefore = true;
                    existingVolunteer.FirstName = newVolunteer.FirstName;
                    existingVolunteer.LastName = newVolunteer.LastName;
                    existingVolunteer.Email = newVolunteer.Email;
                    existingVolunteer.Phone = newVolunteer.Phone;
                    existingVolunteer.OrganizationOrGroup = newVolunteer.OrganizationOrGroup;
                    existingVolunteer.Message = newVolunteer.Message;

                    if (SelectedEvent.EventType == EventType.Delivery)
                    {
                        existingVolunteer.VehicleType = newVolunteer.VehicleType;
                    }

                    var updateResult = await _svcVolunteer.UpdateAsync(existingVolunteer);

                    if (!updateResult.Success)
                    {
                        Log.Logger.Error($"Error UpdateVolunteer, updateResult: {updateResult.Message}");
                        ShowMessage(updateResult.Message);
                        return false;
                    }
                    newVolunteer = updateResult.Data;
                }
                else
                {
                    isNewVolunteer = true;
                    var createResult = await _svcVolunteer.CreateAsync(newVolunteer);

                    if (!createResult.Success)
                    {
                        Log.Logger.Error($"Error UpdateVolunteer, updateResult: {createResult.Message}");
                        ShowMessage(createResult.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {

                Log.Logger.Error(ex, $"Error UpdateVolunteer: {ex.Message}");
                ShowMessage(ex.Message);
                return false;
            }

            return true;
        }



        private async Task CreateFinalMessage()
        {
            string ResultTitle;
            string ResultSubTitle = string.Empty;
            string ResultMessage = string.Empty;

            DisplaySearch = DisplayNone;
            DisplayForm = DisplayNone;

            var locationResult = await _svcLocation.GetByIdAsync(selectedLocation);
            string selectedLocationName = locationResult.Data.Name;

            if (isNewVolunteer)
            {
                ResultTitle = _lc.Keys["VolunteerSignUpNew", new { fullName = newVolunteer.FullName }] + "<br />";
                ResultSubTitle = _lc.Keys["VolunteerSignUpNewSubtitle"] + " ";
            }
            else // new Volunteer
            {
                ResultTitle = _lc.Keys["VolunteerSignUpAgain", new { fullName = newVolunteer.FullName }] + "<br />";
                ResultSubTitle = _lc.Keys["VolunteerSignUpAgainSubtitle"] + " ";
            }

            string startTime = $"{SelectedEvent.EventDateScheduled.ToShortDateString()} {SelectedEvent.EventDateScheduled.ToShortTimeString()}";
            string endTime = SelectedEvent.EventDateScheduled.AddHours(SelectedEvent.EventDurationHours).ToShortTimeString();
            ResultMessage +=
                $"{_lc.Keys["WeReceivedYourRegistration"]} {_translateLogic.GetTranslation(SelectedEvent.EventName)}, ";
            ResultMessage += $"{_lc.Keys["FromStartTimeToEndTime", new {startTime = startTime, endTime = endTime}]} ";
            ResultMessage += $"{_lc.Keys["AtBedBrigadeLocation", new { locationName = selectedLocationName }]} <br />";
            ResultMessage += $"{SelectedEvent.EventNote} <br /><br />";
            ResultMessage += $"{_lc.Keys["WeLookForwardToSeeingYou"]} <br />";

            ResultMessage += $"{_lc.Keys["ThankYou"]}<br />";
            ResultMessage += selectedLocationName;
            ResultDisplay = "";
            FinalMessage = BootstrapHelper.GetBootstrapJumbotron(ResultTitle, ResultSubTitle, ResultMessage);
        } // Create Final Message

        private async Task<bool> UpdateScheduleVolunteerCount()
        {
            try
            {
                var existingResult = await _svcSchedule.GetByIdAsync(SelectedEvent.ScheduleId);
                if (!existingResult.Success)
                {
                    ShowMessage(existingResult.Message);
                    return false;
                }

                var existingSchedule = existingResult.Data;
                existingSchedule.VolunteersRegistered += 1;

                if (newVolunteer.VehicleType != VehicleType.None)
                {
                    existingSchedule.DeliveryVehiclesRegistered += 1;
                }

                var updateResult = await _svcSchedule.UpdateAsync(existingSchedule);

                if (!updateResult.Success)
                {
                    Log.Logger.Error($"Error UpdateScheduleVolunteerCount, updateResult: {updateResult.Message}");
                    ShowMessage(updateResult.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error UpdateScheduleVolunteerCount: {ex.Message}");
                ShowMessage(ex.Message);
                return false;
            }

            return true;
        } 

        #endregion

    }
}
