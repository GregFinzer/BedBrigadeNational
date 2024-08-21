using Syncfusion.Blazor;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.DropDowns;
using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
using ChangeEventArgs = Microsoft.AspNetCore.Components.ChangeEventArgs;
using BedBrigade.Data.Models;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using System.Data.Entity;
using Syncfusion.Blazor.RichTextEditor;
using System.Data.Entity.Infrastructure;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Enums;
using Microsoft.AspNetCore.WebUtilities;

namespace BedBrigade.Client.Components.Pages
{
    public partial class VolunteerSignUp : ComponentBase
    {
        #region Declaration

        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IVolunteerDataService? _svcVolunteer { get; set; }
        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject] private IVolunteerEventsDataService? _svcVolunteerEvents { get; set; }
        [Inject] private NavigationManager? _nav { get; set; }

        private BedBrigade.Data.Models.Volunteer? newVolunteer;

        private List<Location> Locations { get; set; } = new List<Location>();
        private List<Schedule> Events { get; set; } = new List<Schedule>(); // Full List of Events
        private List<Volunteer> Volunteers { get; set; } = new List<Volunteer>(); // Full List of registered Volunteers
        private List<Schedule> LocationEvents { get; set; } = new List<Schedule>(); // Selected Location Events
        private Schedule? SelectedEvent { get; set; } = new Schedule(); // selected Event

        private List<VolunteerEvent> VolunteerRegister { get; set; } = new List<VolunteerEvent>(); // Volunteer/Events Registration

        private SearchLocation? SearchLocation;

        private const string DisplayNone = "none";
        private const string AlertDanger = "alert alert-danger";
        private const string FormMessage = "Please fill out all the mandatory fields marked with an asterisk (*).";
        private const string FormNotCompleted = FormMessage; //"The Bed Request Form is not completed!<br />"+FormMessage;
        private const string FormCompleted = "The Volunteer Form is completed!";
        private const string AlertSuccess = "success";
        private const string AlertWarning = "warning";

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
        private MarkupString LocationEventsAlert = BootstrapHelper.GetBootstrapMessage("warning", "Sorry, no available volunteer events in selected Location.<br/>Please select other location or try again later.", "", false);
        public int NumericValue { get; set; } = 1;

        private MarkupString FinalMessage;
        private MarkupString NotificationMessage = BootstrapHelper.GetBootstrapMessage("info", FormMessage, "", false);
        private string SubmitAlertMessage = string.Empty;
        private string AlertDisplay = DisplayNone;
        private string ResultDisplay = DisplayNone;
        private string NotificationDisplay = string.Empty;
        private string AlertType = AlertDanger;

        private ReCAPTCHA? reCAPTCHAComponent;
        private bool ValidReCAPTCHA = false;
        private bool ServerVerificatiing = false;
        private bool EditFormStatus = false; // true if not errors
        private int selectedLocation = 0;
        private string selectedLocationName = string.Empty;
        private string selectedEventName = string.Empty;
        private int EventCutOffTimeDays = 4; // take from Configuration
        private string VolunteerEventNote = string.Empty;
        private int exVolunteerId = 0;
        private bool isRegisterNow = false;

        private bool DisableSubmitButton => !ValidReCAPTCHA || ServerVerificatiing;
        private EditContext? EC { get; set; }

        private string? _locationQueryParm;

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

        #endregion
        #region Initialization

        protected override void OnInitialized()
        {
            var uri = _nav.ToAbsoluteUri(_nav.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("location", out var locationQueryParm))
            {
                _locationQueryParm = locationQueryParm;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            newVolunteer = new BedBrigade.Data.Models.Volunteer();
            EC = new EditContext(newVolunteer);
            //messageStore = new ValidationMessageStore(EC);
            
            var dataEvents = await _svcSchedule.GetAllAsync();
            if (dataEvents.Success && dataEvents != null)
            {
                Events = dataEvents.Data;
            }

            var dataRegister = await _svcVolunteerEvents.GetAllAsync();
            if (dataRegister.Success && dataRegister != null)
            {
                VolunteerRegister = dataRegister.Data;
            }

            var dataVolunteers = await _svcVolunteer.GetAllAsync();
            if (dataVolunteers != null)
            {
                Volunteers = dataVolunteers.Data.ToList();
            }

            var dataLocations = await _svcLocation.GetAllAsync();
            if (dataLocations != null)
            {
                Locations = dataLocations.Data.ToList();
            }

        } // Init

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
                selectedLocation = SearchLocation.ddlValue;
                selectedLocationName = Locations.SingleOrDefault(item => item.LocationId == selectedLocation).Name;
                GetLocationEvents();

            }
        } // check child component data


        #endregion

        #region Validation & Events


        private void HandleSelectedValueChanged(string strLocationId)
        {
            //Select Location in drop-down list
            selectedLocation = Convert.ToInt32(strLocationId);
            selectedLocationName = Locations.SingleOrDefault(item => item.LocationId == selectedLocation).Name;
            GetLocationEvents();

        }

        private void onPreviousVolunteer(Microsoft.AspNetCore.Components.ChangeEventArgs args)
        {
            DisplayExistingMessage = DisplayNone;
            if (newVolunteer.IHaveVolunteeredBefore)
            {
                DisplayExistingMessage = "";
            }
        }

        private void RegisterNow(Microsoft.AspNetCore.Components.ChangeEventArgs args)
        {
            if (isRegisterNow)
            {
                // GetLocationEvents();
            }
            else
            {
                //DisplayLocationEvents = DisplayNone;
                //DisplayLocationStatusMessage = DisplayNone;
            }
        }

        private void GetLocationEvents()
        { // Filter Events to selected Location & Events with available dates & registered Volunteers < VolunteersMax
            try
            {


                DisplayLocationEvents = DisplayNone;
                DisplayLocationStatusMessage = DisplayNone;
                // Date Filtration
                LocationEvents = Events
                    .Where(item => item.LocationId == selectedLocation) // Location
                    .Where(item => item.EventStatus == EventStatus.Scheduled) // Scheduled Events
                    .Where(item => item.EventDateScheduled > DateTime.Today.AddDays(EventCutOffTimeDays)) // CutOffDays
                    .ToList();

                // update volunteer count

                foreach (var item in LocationEvents)
                {
                    item.VolunteersRegistered = VolunteerRegister
                            .Where(reg => reg.LocationId == selectedLocation)
                            .Where(reg => reg.ScheduleId == item.ScheduleId)
                            .ToList().Count();

                }

                // additional filter to registered volunteers

                LocationEvents = LocationEvents
                    .Where(item => item.VolunteersRegistered < item.VolunteersMax)
                    .ToList();

                //EnumHelper.OutputObjectProperty(LocationEvents);


                SelectedEvent = new Schedule();
                DisplayEventDetail = DisplayNone;

                if (LocationEvents.Count > 0)
                {
                    DisplayLocationEvents = "";
                }
                else
                {
                    DisplayLocationStatusMessage = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }//GetLocationEvents

        private void ChangeEvent(ChangeEventArgs<int, Schedule> args)
        {
            var intSchedulId = args.Value;
            SelectedEvent = LocationEvents.Where(item => item.ScheduleId == intSchedulId).FirstOrDefault();
            selectedEventName = SelectedEvent.EventName;
            DisplayEventDetail = "";
        }

        private void RunValidation()
        {

            NotificationMessage = (MarkupString)"&nbsp;";
            DisplayAddressMessage = DisplayNone;
            NotificationDisplay = "";
            EditFormStatus = EC.Validate(); // manually trigger the validation here

            FormValidation();
        } // Run Validation

        private bool CheckEmailAddress(string eMail)
        {
            DisplayExId = DisplayNone;
            var bEmailFound = false;

            // return true if found email address
            var exVolunteer = Volunteers.Where(item => item.Email == eMail).FirstOrDefault();
            if (exVolunteer != null)
            {
                bEmailFound = true;
                if (newVolunteer.IHaveVolunteeredBefore) // Volunteer found by eMail
                {
                    exVolunteerId = exVolunteer.VolunteerId;
                    newVolunteer.VolunteerId = exVolunteer.VolunteerId;
                    newVolunteer.CreateDate = exVolunteer?.CreateDate;
                    newVolunteer.CreateUser = exVolunteer?.CreateUser;
                    newVolunteer.UpdateDate = exVolunteer?.UpdateDate;
                    newVolunteer.LocationId = exVolunteer.LocationId;
                    DisplayExId = "";
                    // load data to form
                    // if (newVolunteer.FirstName.Trim().Length == 0 || newVolunteer.LastName.Trim().Length == 0)
                    // {  // Copy all properties!
                    // newVolunteer.VolunteerId = exVolunteer.VolunteerId;
                    // newVolunteer.FirstName = exVolunteer.FirstName;
                    // newVolunteer.LastName = exVolunteer.LastName;
                    //  newVolunteer.Email = exVolunteer.Email;
                    //  newVolunteer.Phone = exVolunteer.Phone;
                    //  newVolunteer.Message = exVolunteer.Message;
                    //   newVolunteer.OrganizationOrGroup = exVolunteer.OrganizationOrGroup;
                    //newVolunteer.IHaveAMinivan = exVolunteer.IHaveAMinivan;
                    // newVolunteer.IHaveAnSUV = exVolunteer.IHaveAnSUV;
                    // newVolunteer.IHaveAPickupTruck = exVolunteer.IHaveAPickupTruck;


                    // }
                }
            }

            return bEmailFound;
        } // check email address

        private bool EmailAddressValid()
        {
            DisplayEmailMessage = DisplayNone;
            if (newVolunteer.Email.Length > 0) // Email entered
            {
                var bEmailFound = CheckEmailAddress(newVolunteer.Email);

                // Debug.WriteLine(newVolunteer.Email + ": " + bEmailFound.ToString());


                if (newVolunteer.IHaveVolunteeredBefore) // existing Account
                {
                    if (bEmailFound)
                    { // email & volunteer found
                        ExistingMessage = BootstrapHelper.GetBootstrapMessage(AlertSuccess, "We found your email. Welcome back. You shoul re-enter your profile information.", "", false, "compact");

                    }
                    else
                    {
                        ExistingMessage = BootstrapHelper.GetBootstrapMessage(AlertWarning, "We could not find your email. Please check again or make new reqistratioin.", "", false, "compact");
                    }
                    DisplayExistingMessage = "";
                }
                else
                {
                    if (bEmailFound) // email already used
                    {
                        DisplayEmailMessage = "";// Form is not completed
                        return false;
                    }
                }
            } // email special validation
            else
            {
                return false;
            } // No email entered

            return true;

        } // Email Address Valid



        private void FormValidation()
        {
            EditFormStatus = false;
            if (!EmailAddressValid())
            {
                return;
            };

            EditFormStatus = EC.Validate();

            if (EditFormStatus)
            {
                if (ValidReCAPTCHA)
                {
                    NotificationMessage = BootstrapHelper.GetBootstrapMessage(AlertSuccess, FormCompleted);
                }
                else
                {
                    NotificationMessage = BootstrapHelper.GetBootstrapMessage(AlertSuccess, FormCompleted, "Please check reCAPTCHA.");
                }
            }
            else
            {
                EditFormStatus = false;
                NotificationMessage = BootstrapHelper.GetBootstrapMessage(AlertWarning, FormNotCompleted);

            }
        } // Form Validation


        #endregion    


        #region reCaptcha


        private void OnSuccess()
        {
            //ResultMessage = "Secret Verification OK";
            //ResultDisplay = "";
            ValidReCAPTCHA = true;
            if (EditFormStatus)
            {
                NotificationDisplay = DisplayNone;
            }
            else
            {
                NotificationMessage = BootstrapHelper.GetBootstrapMessage("error", FormNotCompleted);
                NotificationDisplay = "";
            }
        } // reCaptcha success

        private void OnExpired()
        {
            ValidReCAPTCHA = false;
        }

        #endregion
        #region SaveVolunteer


        private async Task SaveVolunteer()
        {
            var FormStatusMessage = "The Volunteer Form is completed.";
            RunValidation();

            if (EditFormStatus && ValidReCAPTCHA) // data are valid
            {
                NotificationMessage = BootstrapHelper.GetBootstrapMessage("success", "The Form is completed");
                NotificationDisplay = "";
                if (!newVolunteer.IHaveVolunteeredBefore)
                {
                    newVolunteer.LocationId = selectedLocation; // get value from child component
                }

                await UpdateDatabase();

            } // Edit Form Status
            else // not valid data or/and reCaptcha
            {
                FormStatusMessage = string.Empty;
                var ReCaptchaStatusMessage = string.Empty;

                if (EditFormStatus)
                {
                    FormStatusMessage = "The Bed Request Form is completed!";
                    AlertType = "success";
                    if (!ValidReCAPTCHA)
                    {
                        AlertType = AlertWarning;
                        ReCaptchaStatusMessage = "Please check reCAPTCHA!";
                    }
                }
                else
                {
                    FormStatusMessage = FormNotCompleted;
                    AlertType = AlertWarning;
                }

                NotificationMessage = BootstrapHelper.GetBootstrapMessage(AlertType, FormStatusMessage, ReCaptchaStatusMessage);
                NotificationDisplay = "";
            }


        } // Save Request

        private async Task UpdateDatabase()
        {
            var intNewVolunteerId = 0;
            var intNewRegistrationId = 0;
            var newRegister = new VolunteerEvent();

            ServiceResponse<Volunteer> addResult;
            string ResultTitle = "Volunteer Registration";
            string ResultSubTitle = string.Empty;
            string ResultMessage = string.Empty;
            var intAddCar = 0;

            // Step 1 - save Volunteer Data
            try
            {
                if (newVolunteer.VolunteerId > 0) // update Volunteer Data
                {
                    addResult = await _svcVolunteer.UpdateAsync(newVolunteer);
                }
                else // create new Volunteer
                {
                    addResult = await _svcVolunteer.CreateAsync(newVolunteer);
                }

                if (addResult.Success && addResult.Data != null)
                {
                    newVolunteer = addResult.Data; // saved Volunteer Data                   

                }

                if (newVolunteer != null && newVolunteer.VolunteerId > 0)
                {
                    await ReviewVolunteerData(newVolunteer);
                }
                else
                {
                    SubmitAlertMessage = "Warning! Unable to add new Volunteer!";
                    AlertType = AlertDanger;
                    AlertDisplay = "";
                }
            }
            catch (Exception ex)
            {
                AlertType = AlertDanger;
                SubmitAlertMessage = "Error! " + ex.Message;
                AlertDisplay = "";
            }
        } // update database

        private async Task ReviewVolunteerData(Volunteer newVolunteer)
        {
            var intNewVolunteerId = 0;
            var intNewRegistrationId = 0;
            var newRegister = new VolunteerEvent();

            string ResultTitle = "Volunteer Registration";
            string ResultSubTitle = string.Empty;
            string ResultMessage = string.Empty;
            var intAddCar = 0;

            intNewVolunteerId = newVolunteer.VolunteerId;
            // Step 2 - Register Volunteer to Event
            if (isRegisterNow)
            {
                if (SelectedEvent != null && SelectedEvent.ScheduleId > 0)
                {
                    //newRegister = new VolunteerEvent();
                    newRegister.VolunteerId = intNewVolunteerId;
                    newRegister.LocationId = selectedLocation;
                    newRegister.ScheduleId = SelectedEvent.ScheduleId;
                    // comment
                    newRegister.VolunteerEventNote = VolunteerEventNote;
                    var addRegister = await _svcVolunteerEvents.CreateAsync(newRegister);
                    if (addRegister.Success && addRegister.Data != null)
                    {
                        newRegister = addRegister.Data; // added Request
                        intNewRegistrationId = newRegister.RegistrationId;
                        if (Convert.ToInt32(newVolunteer.VehicleType) > 0) // not noCar
                        {
                            intAddCar = 1;
                        }

                        await UpdateScheduleVolunteerCount(newRegister.ScheduleId, intAddCar);
                    }
                }
            } // register now

            CreateFinalMessage(newVolunteer, newRegister, intNewRegistrationId);

        } // Review Volunteer Data

        private void CreateFinalMessage(Volunteer newVolunteer, VolunteerEvent newRegister, int intNewRegistrationId)
        {
            string ResultTitle = "Volunteer Registration";
            string ResultSubTitle = string.Empty;
            string ResultMessage = string.Empty;

            //AlertType = "alert alert-success";
            DisplaySearch = DisplayNone;
            DisplayForm = DisplayNone;

            // ResultMessage = "New Bed Request #" + newVolunteer.BedRequestId.ToString() + " created Successfully!<br />";

            if (exVolunteerId > 0)
            {
                ResultTitle = "Welcome back, " + newVolunteer.FullName + "!";
                ResultSubTitle = "Thank you so much for your enthusiasm and genuine commitment to assisting us.";
            }
            else // new Volunteer
            {
                ResultTitle = "Welcome, " + newVolunteer.FullName + ".<br />";
                ResultSubTitle = "We appreciate your wish to join Bed Brigade Volunteer Team (your Volunteer ID: " + newVolunteer.VolunteerId.ToString() + ").";
            }

            if (isRegisterNow && intNewRegistrationId > 0)
            {
                ResultMessage += "Also, we received your registration as participant to scheduled Event:<br />" + selectedEventName + " (ID=" + newRegister.ScheduleId.ToString() + "), ";
                ResultMessage += SelectedEvent.EventDateScheduled.ToShortDateString() + "&nbsp;" + SelectedEvent.EventDateScheduled.ToShortTimeString();
                ResultMessage += "<br /> at Bed Brigade Location: " + selectedLocationName + " (ID=" + newRegister.LocationId.ToString() + ").";
                ResultMessage += "<br />We will look over your Bed Brigade Event registration and reply by email as soon as possible.<br />";
            }
            else // No event Registration
            {
                var strDisplayLocationName = Locations.SingleOrDefault(item => item.LocationId == newVolunteer.LocationId).Name;
                ResultSubTitle += "<br />Your Bed Brigade Location is: <b>" + strDisplayLocationName + "</b>&nbsp;(ID = " + newVolunteer.LocationId.ToString() + ")";
            }
            ResultMessage += "Thank you!<br />Bed Brigade";
            ResultDisplay = "";
            FinalMessage = BootstrapHelper.GetBootstrapJumbotron(ResultTitle, ResultSubTitle, ResultMessage);
        } // Create Final Message

        private async Task UpdateScheduleVolunteerCount(int intScheduleId, int intAddCar = 0)
        {
            // update Registered volunteer count for registered Schedule
            var dataRegister = await _svcVolunteerEvents.GetAllAsync(); // all registered events
            if (dataRegister.Success && dataRegister != null)
            {
                VolunteerRegister = dataRegister.Data;
            }
            var registerEvent = new Schedule();

            var dataEvents = await _svcSchedule.GetByIdAsync(intScheduleId); // Get selected Schedule data
            if (dataEvents.Success && dataEvents != null)
            {
                registerEvent = dataEvents.Data; // dataEvents.Data.ToList().FirstOrDefault(item => item.ScheduleId == intScheduleId);
                // update registered Volunteers by re-count of all Volunteers already registered for selected event
                registerEvent.VolunteersRegistered = VolunteerRegister.Where(reg => reg.ScheduleId == intScheduleId).ToList().Count();
                // update Registered car for event
                registerEvent.VehiclesDeliveryRegistered = registerEvent.VehiclesDeliveryRegistered + intAddCar;

                var addRegCount = await _svcSchedule.UpdateAsync(registerEvent);
            }
        } // Update Volunteer Count

        #endregion

    }
}
