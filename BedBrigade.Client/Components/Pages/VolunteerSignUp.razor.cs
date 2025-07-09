using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using BedBrigade.SpeakIt;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Inputs;
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
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private ISpokenLanguageDataService _svcSpokenLanguage { get; set; }

        [Inject] private IEmailBuilderService _svcEmailBuilder { get; set; }
        [Inject] private ISendSmsLogic _sendSmsLogic { get; set; }

        [Parameter] public string? LocationRoute { get; set; }
        [Parameter] public int? ScheduleId { get; set; }

        private Volunteer? newVolunteer;
        private List<Schedule> LocationEvents { get; set; } = new List<Schedule>(); // Selected Location Events
        private Schedule? SelectedEvent { get; set; } // selected Event

        private SearchLocation? SearchLocation;

        private const string DisplayNone = "none";

        private string DisplayForm = DisplayNone;
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
        private string ResultDisplay = DisplayNone;
        private bool ValidReCAPTCHA = false;
        private int selectedLocation = 0;
        private bool isNewVolunteer = false;


        private EditContext? EC { get; set; }

        private string MyMessage = string.Empty;
        private string MyMessageDisplay = DisplayNone;
        private bool AreSignUpsAvailable = false;
        private SfDropDownList<int, Schedule> ddlSchedule;

        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "4" },
        };

        [Parameter] public string PreloadLocation { get; set; }
        private ValidationMessageStore _validationMessageStore;

        private List<SpokenLanguage> SpokenLanguages { get; set; } = [];
        private string[] SelectedLanguages { get; set; } = [];
        public required SfMaskedTextBox phoneTextBox;
        private int _previousNumberOfVolunteers = 0;
        private VehicleType? _previousDeliveryVehicle;
        #endregion

        #region Initialization

        protected override void OnInitialized()
        {
            _lc.InitLocalizedComponent(this);
            newVolunteer = new Volunteer();
            EC = new EditContext(newVolunteer);
            _validationMessageStore = new ValidationMessageStore(EC);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    SpokenLanguages = (await _svcSpokenLanguage.GetAllAsync()).Data;

                    if (!string.IsNullOrEmpty(LocationRoute))
                    {
                        var result = await _svcLocation.GetLocationByRouteAsync(LocationRoute);

                        if (!result.Success || result.Data == null)
                        {
                            _nav.NavigateTo($"/Sorry/{LocationRoute}/Volunteer");
                            return;
                        }

                        await SearchLocation.ForceLocationByName(LocationRoute);
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
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"VolunteerSignUp.OnAfterRenderAsync");
                await ShowMessage("Error loading page, try again later");
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

        public async Task HandlePhoneMaskFocus()
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
        }

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
                    await ShowMessage(response.Message);
                }

                if (LocationEvents.Count > 0)
                {
                    DisplayLocationEvents = "";
                    AreSignUpsAvailable = true;

                    if (response.Data.Any(o => o.ScheduleId == ScheduleId))
                    {
                        ddlSchedule.Value = ScheduleId.Value;
                    }
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
                await ShowMessage("Error getting location events: " + ex.Message);
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

        private async Task ShowMessage(string message)
        {
            MyMessage = message;
            MyMessageDisplay = "";
            await _js.InvokeVoidAsync("BedBrigadeUtil.ScrollToElementId", "myMessage");
        }

        private async Task<bool> IsValid()
        {
            ClearMessage();

            bool formIsValid = ValidationLocalization.ValidateModel(newVolunteer, _validationMessageStore, _lc);

            if (!formIsValid)
            {
                await ShowMessage(_lc.Keys["VolunteerFormNotCompleted"]);
                return false;
            }

            bool isPhoneValid = Validation.IsValidPhoneNumber(newVolunteer.Phone);

            if (!isPhoneValid)
            {
                _validationMessageStore.Add(new FieldIdentifier(newVolunteer, nameof(newVolunteer.Phone)), _lc.Keys["ValidPhoneNumber"]);
                await ShowMessage(_lc.Keys["VolunteerFormNotCompleted"]);
                return false;
            }

            var emailResult = Validation.IsValidEmail(newVolunteer.Email);
            if (!emailResult.IsValid)
            {
                _validationMessageStore.Add(new FieldIdentifier(newVolunteer, nameof(newVolunteer.Email)), emailResult.UserMessage);
                await ShowMessage(_lc.Keys["ValidEmail"]);
                return false;
            }

            if (SelectedEvent == null)
            {
                await ShowMessage(_lc.Keys["PleaseSelectAnEvent"]);
                return false;
            }

            if (!ValidReCAPTCHA)
            {
                await ShowMessage(_lc.Keys["PleaseCheckRecaptcha"]);
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
            if (!await IsValid())
            {
                return;
            }

            await UpdateDatabase();
        } 

        private async Task UpdateDatabase()
        {
            newVolunteer.OtherLanguagesSpoken = string.Join(", ", SelectedLanguages);

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
                var existingSignUpResult = await _svcSignUp.GetByVolunteerEmailAndScheduleId(newVolunteer.VolunteerId, SelectedEvent.ScheduleId);

                if (existingSignUpResult.Success && existingSignUpResult.Data != null)
                {
                    return await UpdateExistingSignup(existingSignUpResult.Data);
                }

                return await CreateNewSignup();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error ScheduleVolunteer: {ex.Message}");
                await ShowMessage("Error ScheduleVolunteer: " + ex.Message);
                return false;
            }
        }

        private async Task<bool> UpdateExistingSignup(SignUp signUp)
        {
            _previousNumberOfVolunteers = signUp.NumberOfVolunteers;
            _previousDeliveryVehicle = signUp.VehicleType;

            signUp.SignUpNote = newVolunteer.Message;
            signUp.NumberOfVolunteers = newVolunteer.NumberOfVolunteers;
            var updateResult = await _svcSignUp.UpdateAsync(signUp);

            if (!updateResult.Success)
            {
                await ShowMessage(updateResult.Message);
                Log.Logger.Error($"Error ScheduleVolunteer: {updateResult.Message}");
                return false;
            }

            string customMessage = "This is to confirm that your sign-up was updated.";
            var emailResponse = await _svcEmailBuilder.SendSignUpConfirmationEmail(updateResult.Data, customMessage);

            if (!emailResponse.Success)
            {
                await ShowMessage(emailResponse.Message);
                Log.Logger.Error($"Error SendSignUpConfirmationEmail: {emailResponse.Message}");
                return false;
            }

            return true;
        }

        private async Task<bool> CreateNewSignup()
        {
            SignUp newRegister = new SignUp();
            newRegister.ScheduleId = SelectedEvent.ScheduleId;
            newRegister.VolunteerId = newVolunteer.VolunteerId;
            newRegister.LocationId = selectedLocation;
            newRegister.SignUpNote = newVolunteer.Message;
            newRegister.NumberOfVolunteers = newVolunteer.NumberOfVolunteers;
            var createResult = await _svcSignUp.CreateAsync(newRegister);

            if (!createResult.Success)
            {
                await ShowMessage(createResult.Message);
                Log.Logger.Error($"Error ScheduleVolunteer: {createResult.Message}");
                return false;
            }

            string customMessage = "This is to confirm that your sign-up was created.";
            var emailResponse = await _svcEmailBuilder.SendSignUpConfirmationEmail(createResult.Data, customMessage);

            if (!emailResponse.Success)
            {
                await ShowMessage(emailResponse.Message);
                Log.Logger.Error($"Error SendSignUpConfirmationEmail: {emailResponse.Message}");
                return false;
            }

            var smsResponse = await _sendSmsLogic.CreateSignUpReminder(createResult.Data);

            if (!smsResponse.Success)
            {
                await ShowMessage(smsResponse.Message);
                Log.Logger.Error($"Error CreateSignUpReminder: {smsResponse.Message}");
                return false;
            }
            return true;
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
                    existingVolunteer.NumberOfVolunteers = newVolunteer.NumberOfVolunteers;

                    if (SelectedEvent.EventType == EventType.Delivery)
                    {
                        existingVolunteer.VehicleType = newVolunteer.VehicleType;
                    }

                    var updateResult = await _svcVolunteer.UpdateAsync(existingVolunteer);

                    if (!updateResult.Success)
                    {
                        Log.Logger.Error($"Error UpdateVolunteer, updateResult: {updateResult.Message}");
                        await ShowMessage(updateResult.Message);
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
                        await ShowMessage(createResult.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error UpdateVolunteer: {ex.Message}");
                await ShowMessage("Error UpdateVolunteer: " + ex.Message);
                return false;
            }

            return true;
        }



        private async Task CreateFinalMessage()
        {
            try
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
                else if (_previousNumberOfVolunteers > 0)
                {
                    //TODO:  Translate this
                    ResultTitle = "Sign-up Updated for " + newVolunteer.FullName + "<br />";
                    ResultSubTitle = "We appreciate you serving with the Bed Brigade." + " ";
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
                ResultMessage += $"{_lc.Keys["FromStartTimeToEndTime", new { startTime = startTime, endTime = endTime }]} ";
                ResultMessage += $"{_lc.Keys["AtBedBrigadeLocation", new { locationName = selectedLocationName }]} <br />";
                ResultMessage += $"{SelectedEvent.EventNote} <br /><br />";
                ResultMessage += $"{_lc.Keys["WeLookForwardToSeeingYou"]} <br />";

                ResultMessage += $"{_lc.Keys["ThankYou"]}<br />";
                ResultMessage += selectedLocationName;
                ResultDisplay = "";
                FinalMessage = BootstrapHelper.GetBootstrapJumbotron(ResultTitle, ResultSubTitle, ResultMessage);
                await _js.InvokeVoidAsync("BedBrigadeUtil.ScrollPastImages");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error CreateFinalMessage: {ex.Message}");
                await ShowMessage("Error CreateFinalMessage: " + ex.Message);
            }

        } // Create Final Message

        private async Task<bool> UpdateScheduleVolunteerCount()
        {
            try
            {
                var existingResult = await _svcSchedule.GetByIdAsync(SelectedEvent.ScheduleId);
                if (!existingResult.Success)
                {
                    await ShowMessage(existingResult.Message);
                    return false;
                }

                var existingSchedule = existingResult.Data;
                existingSchedule.VolunteersRegistered += newVolunteer.NumberOfVolunteers - _previousNumberOfVolunteers;

                if (_previousDeliveryVehicle != null)
                {
                    if (_previousDeliveryVehicle != VehicleType.None && newVolunteer.VehicleType == VehicleType.None)
                    {
                        existingSchedule.DeliveryVehiclesRegistered -= 1;
                    }
                }
                else if (newVolunteer.VehicleType != VehicleType.None)
                {
                    existingSchedule.DeliveryVehiclesRegistered += 1;
                }

                var updateResult = await _svcSchedule.UpdateAsync(existingSchedule);

                if (!updateResult.Success)
                {
                    Log.Logger.Error($"Error UpdateScheduleVolunteerCount, updateResult: {updateResult.Message}");
                    await ShowMessage(updateResult.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error UpdateScheduleVolunteerCount: {ex.Message}");
                await ShowMessage("Error UpdateScheduleVolunteerCount: " + ex.Message);
                return false;
            }

            return true;
        }

        #endregion

        #region Unregister Volunteer

        private async Task UnregisterVolunteer()
        {
            if (!await IsValid())
            {
                return;
            }

            try
            {
                var unregisterResponse = await _svcSignUp.Unregister(newVolunteer.Email, SelectedEvent.ScheduleId);

                if (!unregisterResponse.Success)
                {
                    Log.Logger.Error($"Error UnregisterVolunteer: {unregisterResponse.Message}");
                    await ShowMessage(unregisterResponse.Message);
                    return;
                }

                string customMessage = "This is to confirm that your sign-up was removed.";
                var emailResponse = await _svcEmailBuilder.SendSignUpConfirmationEmail(unregisterResponse.Data, customMessage);

                if (!emailResponse.Success)
                {
                    await ShowMessage(emailResponse.Message);
                    Log.Logger.Error($"Error SendSignUpConfirmationEmail: {emailResponse.Message}");
                    return;
                }

                DisplaySearch = DisplayNone;
                DisplayForm = DisplayNone;
                ResultDisplay = "";
                FinalMessage = BootstrapHelper.GetBootstrapJumbotron("Unregisterd", "You have sucessfully unregistered for the event", string.Empty);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error UnregisterVolunteer: {ex.Message}");
                await ShowMessage("Error UnregisterVolunteer: " + ex.Message);
            }
        }
        #endregion
    }
}
