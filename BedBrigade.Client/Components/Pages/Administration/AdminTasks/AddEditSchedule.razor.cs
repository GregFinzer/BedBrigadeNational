using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Migrations;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using StringUtil = BedBrigade.Common.Logic.StringUtil;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks
{
    public partial class AddEditSchedule : ComponentBase
    {
        [Parameter] public int? ScheduleId { get; set; }
        [Parameter] public int? LocationId { get; set; }

        [Inject] private NavigationManager _nav { get; set; }
        [Inject] private ToastService _toast { get; set; }
        [Inject] private IScheduleDataService _svcSchedule { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IAuthService _svcAuth { get; set; }
        [Inject] private IUserDataService _svcUser { get; set; }
        [Inject] private IConfigurationDataService _svcConfig { get; set; }
        [Inject] private IBedRequestDataService _svcBedRequest { get; set; }
        [Inject] private ISignUpDataService _svcSignUp { get; set; }
        [Inject] private IEmailQueueDataService _svcEmailQueue { get; set; }
        [Inject] private ISmsQueueDataService _svcSmsQueue { get; set; }
        [Inject] private IEmailBuilderService _svcEmailBuilder { get; set; }
        [Inject] private ISendSmsLogic _sendSmsLogic { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public Common.Models.Schedule Model { get; set; } = new();
        public List<Location>? Locations { get; private set; }
        public bool CanSetLocation { get; private set; }
        public string CurrentLocationName { get; private set; } = string.Empty;

        public List<EventStatusEnumItem>? EventStatuses { get; private set; }
        public List<EventTypeEnumItem>? EventTypes { get; private set; }

        public DateTime ScheduleStartDate { get; set; }
        public DateTime ScheduleStartTime { get; set; }

        public string HeaderText => ScheduleId.HasValue ? $"Edit Schedule" : "Add Schedule";
        public string ButtonText => ScheduleId.HasValue ? "Update" : "Add";
        private List<UsState>? StateList = AddressHelper.GetStateList();
        private User? _currentUser = new User();

        private bool _isFromBedRequest = false;
        private bool _showVerificationDialog = false;
        private DateTime? _originalEventDateScheduled;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Check if coming from bed request
                var uri = _nav.ToAbsoluteUri(_nav.Uri);
                _isFromBedRequest = uri.Query.Contains("fromBedRequest=true");

                // Permissions
                CanSetLocation = _svcUser.IsUserNationalAdmin();

                await LoadLocations();
                await LoadUserData();
                await LoadModel();

                // Enum lists
                EventStatuses = EnumHelper.GetEventStatusItems();
                EventTypes = EnumHelper.GetEventTypeItems();

                // Show verification dialog if coming from bed request
                if (_isFromBedRequest && ScheduleId.HasValue)
                {
                    _showVerificationDialog = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing AddEditSchedule component");
                _toast.Error("Error", "An error occurred while loading the schedule data.");
            }
        }

        private async Task LoadUserData()
        {
            Log.Information($"{_svcAuth.UserName} went to the Manage Schedules Page");
            _currentUser = (await _svcUser!.GetCurrentLoggedInUser()).Data;
        } 

        private async Task LoadLocations()
        {
            var result = await _svcLocation.GetAllAsync();
            if (result.Success && result.Data != null)
            {
                Locations = result.Data;
                var userLoc = Locations.FirstOrDefault(l => l.LocationId == _svcAuth.LocationId);
                if (userLoc != null) CurrentLocationName = userLoc.Name;
            }
        }

        private async Task LoadModel()
        {
            //Edit
            if (ScheduleId.HasValue)
            {
                var result = await _svcSchedule.GetByIdAsync(ScheduleId.Value);
                if (result.Success && result.Data != null)
                {
                    Model = result.Data;
                    _originalEventDateScheduled = Model.EventDateScheduled;
                    // Split date/time for editors
                    ScheduleStartDate = Model.EventDateScheduled.Date;
                    ScheduleStartTime = new DateTime(Model.EventDateScheduled.TimeOfDay.Ticks);
                }
                else
                {
                    ErrorMessage = result.Message ?? "Could not load schedule.";
                }
            }
            else //Add
            {
                await BuildModelForAdd();
            }
        }

        private async Task BuildModelForAdd()
        {
            Model = new Common.Models.Schedule();
            Model.LocationId = LocationId ?? _svcAuth.LocationId;
                
            var scheduleResult = await _svcSchedule.GetLastScheduledByLocationIdAndUser(_svcAuth.LocationId);

            if (scheduleResult.Success && scheduleResult.Data != null)
            {
                if (scheduleResult.Data.EventDateScheduled.Date < DateTime.Now.Date)
                {
                    Model.EventDateScheduled = DateUtil.NextSaturday();
                }
                else
                {
                    Model.EventDateScheduled = scheduleResult.Data.EventDateScheduled.Date.AddDays(7);
                }
            }
            else
            {
                Model.EventDateScheduled = DateUtil.NextSaturday();
            }

            if (DateUtil.IsFirstSaturdayOfTheMonth(Model.EventDateScheduled))
            {
                await SetBuildValues(scheduleResult.Data);
            }
            else
            {
                await SetDeliveryValues(scheduleResult.Data);
            }

            ScheduleStartDate = Model.EventDateScheduled.Date;
            ScheduleStartTime = new DateTime(Model.EventDateScheduled.TimeOfDay.Ticks);
        }

        private async Task SetDeliveryValues(Common.Models.Schedule? previousSchedule)
        {
            Model.GroupName = previousSchedule?.GroupName ?? string.Empty;
            Model.EventType = EventType.Delivery;
            Model.EventName = string.IsNullOrWhiteSpace(Model.GroupName) ? "Delivery" : Model.GroupName + " Delivery";

            int defaultMaxVolunteers = await _svcConfig.GetConfigValueAsIntAsync(ConfigSection.Schedule,
                ConfigNames.DefaultDeliveryMaxVolunteers, Model.LocationId);
            Model.VolunteersMax = previousSchedule == null ? defaultMaxVolunteers : previousSchedule.VolunteersMax;

            int defaultHour = await _svcConfig.GetConfigValueAsIntAsync(ConfigSection.Schedule,
                ConfigNames.DefaultDeliveryTime, Model.LocationId);
            Model.EventDateScheduled = previousSchedule == null 
                ? Model.EventDateScheduled.AddHours(defaultHour) 
                : Model.EventDateScheduled.AddHours(previousSchedule.EventDateScheduled.Hour);

            int defaultDuration = await _svcConfig.GetConfigValueAsIntAsync(ConfigSection.Schedule,
                ConfigNames.DefaultDeliveryDurationHours, Model.LocationId);
            Model.EventDurationHours = previousSchedule == null ? defaultDuration : previousSchedule.EventDurationHours;

            Model.EventStatus = EventStatus.Scheduled;

            FillAddressAndOrganizer(previousSchedule);

            string defaultEventNote = await _svcConfig.GetConfigValueAsync(ConfigSection.Schedule,
                ConfigNames.DefaultDeliveryEventNote, Model.LocationId);
            Model.EventNote = previousSchedule != null && previousSchedule.EventType == EventType.Delivery ? previousSchedule.EventNote : defaultEventNote;
        }

        private async Task SetBuildValues(Common.Models.Schedule? previousSchedule)
        {
            Model.EventType = EventType.Build;
            Model.GroupName = previousSchedule?.GroupName ?? string.Empty;
            Model.EventName = string.IsNullOrWhiteSpace(Model.GroupName) ? "Build" : Model.GroupName + " Build";

            int defaultMaxVolunteers = await _svcConfig.GetConfigValueAsIntAsync(ConfigSection.Schedule,
                ConfigNames.DefaultBuildMaxVolunteers, Model.LocationId);
            Model.VolunteersMax = defaultMaxVolunteers;

            int defaultHour = await _svcConfig.GetConfigValueAsIntAsync(ConfigSection.Schedule,
                ConfigNames.DefaultBuildTime, Model.LocationId);
            Model.EventDateScheduled = Model.EventDateScheduled.AddHours(defaultHour);

            int defaultDuration = await _svcConfig.GetConfigValueAsIntAsync(ConfigSection.Schedule,
                ConfigNames.DefaultBuildDurationHours, Model.LocationId);
            Model.EventDurationHours = defaultDuration;

            Model.EventStatus = EventStatus.Scheduled;

            FillAddressAndOrganizer(previousSchedule);

            string defaultEventNote = await _svcConfig.GetConfigValueAsync(ConfigSection.Schedule,
                ConfigNames.DefaultBuildEventNote, Model.LocationId);
            Model.EventNote =defaultEventNote;
        }

        private void FillAddressAndOrganizer(Common.Models.Schedule? previousSchedule)
        {
            Location loc = Locations.First(l => l.LocationId == Model.LocationId);

            Model.Address = string.IsNullOrWhiteSpace(previousSchedule?.Address) 
                ? loc.BuildAddress : previousSchedule!.Address;

            Model.City = string.IsNullOrWhiteSpace(previousSchedule?.City) 
                ? loc.BuildCity : previousSchedule!.City;

            Model.State = string.IsNullOrWhiteSpace(previousSchedule?.State) 
                ? loc.BuildState : previousSchedule!.State;

            Model.PostalCode = string.IsNullOrWhiteSpace(previousSchedule?.PostalCode)
                ? loc.BuildPostalCode : previousSchedule!.PostalCode;

            Model.OrganizerName = string.IsNullOrWhiteSpace(previousSchedule?.OrganizerName) 
                ? Common.Logic.StringUtil.InsertSpaces(_svcAuth.UserName) 
                : previousSchedule!.OrganizerName;

            Model.OrganizerEmail = string.IsNullOrWhiteSpace(previousSchedule?.OrganizerEmail)
                ? _svcAuth.Email
                : previousSchedule!.OrganizerEmail;

            Model.OrganizerPhone = StringUtil.ExtractDigits(string.IsNullOrWhiteSpace(previousSchedule?.OrganizerPhone) ? _svcAuth.Phone : previousSchedule!.OrganizerPhone);
        }


        protected async Task HandleValidSubmit()
        {
            try
            {
                Model.EventDateScheduled = ScheduleStartDate.Date + ScheduleStartTime.TimeOfDay;

                if (ScheduleId.HasValue)
                {
                    var update = await _svcSchedule.UpdateAsync(Model);
                    if (update.Success)
                    {
                        bool remindersRequeued = true;
                        if (_originalEventDateScheduled.HasValue &&
                            _originalEventDateScheduled.Value != Model.EventDateScheduled)
                        {
                            try
                            {
                                remindersRequeued = await RequeueReminders();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Unable to requeue reminders for schedule {ScheduleId}",
                                    Model.ScheduleId);
                                remindersRequeued = false;
                            }
                        }

                        if (remindersRequeued)
                        {
                            _toast.Success("Success", "Schedule updated successfully");
                        }
                        else
                        {
                            _toast.Warning("Schedule Updated",
                                "The schedule was updated, but one or more reminders could not be requeued.");
                        }

                        var redirectUrl = _isFromBedRequest ? "/administration/manage/bedrequests" : "/administration/manage/schedules";
                        _nav.NavigateTo(redirectUrl);
                        return;
                    }
                    ErrorMessage = update.Message;
                    _toast.Error("Error", update.Message);
                    return;
                }

                // Create
                var create = await _svcSchedule.CreateAsync(Model);
                if (create.Success)
                {
                    _toast.Success("Success", "Schedule created successfully");
                    var redirectUrl = _isFromBedRequest ? "/administration/manage/bedrequests" : "/administration/manage/schedules";
                    _nav.NavigateTo(redirectUrl);
                }
                else
                {
                    ErrorMessage = create.Message;
                    _toast.Error("Error", create.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving schedule");
                _toast.Error("Save Schedule", $"An error occurred while saving the schedule: {ex.Message}");
            }

        }

        private async Task<bool> RequeueReminders()
        {
            bool success = true;

            if (Model.EventType == EventType.Delivery)
            {
                var bedRequestsResult = await _svcBedRequest.GetAllForScheduleId(Model.ScheduleId);
                if (!bedRequestsResult.Success || bedRequestsResult.Data == null)
                {
                    Log.Error("Unable to get bed requests for schedule {ScheduleId}: {Message}",
                        Model.ScheduleId, bedRequestsResult.Message);
                    success = false;
                }
                else
                {
                    foreach (var bedRequest in bedRequestsResult.Data)
                    {
                        success &= await RequeueDeliveryReminders(bedRequest);
                    }
                }
            }

            var signUpsResult = await _svcSignUp.GetAllForScheduleIdAsync(Model.ScheduleId);
            if (!signUpsResult.Success || signUpsResult.Data == null)
            {
                Log.Error("Unable to get volunteer signups for schedule {ScheduleId}: {Message}",
                    Model.ScheduleId, signUpsResult.Message);
                return false;
            }

            foreach (var signUp in signUpsResult.Data)
            {
                success &= await RequeueSignUpReminders(signUp);
            }

            return success;
        }

        private async Task<bool> RequeueDeliveryReminders(Common.Models.BedRequest bedRequest)
        {
            var deleteEmailResult = await _svcEmailQueue.DeleteQueuedByBedRequestId(bedRequest.BedRequestId);
            var deleteSmsResult = await _svcSmsQueue.DeleteQueuedSmsByBedRequestId(bedRequest.BedRequestId);
            var emailResult = await _svcEmailBuilder.QueueDeliveryEmailReminder(bedRequest, Model);
            var smsResult = await _sendSmsLogic.QueueDeliverySmsReminder(bedRequest, Model);

            return LogReminderFailures(
                (deleteEmailResult.Success, deleteEmailResult.Message, "delete queued delivery emails"),
                (deleteSmsResult.Success, deleteSmsResult.Message, "delete queued delivery SMS messages"),
                (emailResult.Success, emailResult.Message, "queue the delivery email reminder"),
                (smsResult.Success, smsResult.Message, "queue the delivery SMS reminder"));
        }

        private async Task<bool> RequeueSignUpReminders(SignUp signUp)
        {
            var deleteEmailResult = await _svcEmailQueue.DeleteQueuedBySignUpId(signUp.SignUpId);
            var deleteSmsResult = await _svcSmsQueue.DeleteQueuedBySignUpId(signUp.SignUpId);
            var emailResult = await _svcEmailBuilder.QueueSignUpEmailReminderAsync(signUp);
            var smsResult = await _sendSmsLogic.QueueSignUpSmsReminder(signUp);

            return LogReminderFailures(
                (deleteEmailResult.Success, deleteEmailResult.Message, "delete queued signup emails"),
                (deleteSmsResult.Success, deleteSmsResult.Message, "delete queued signup SMS messages"),
                (emailResult.Success, emailResult.Message, "queue the signup email reminder"),
                (smsResult.Success, smsResult.Message, "queue the signup SMS reminder"));
        }

        private bool LogReminderFailures(params (bool Success, string Message, string Operation)[] results)
        {
            bool success = true;

            foreach (var result in results.Where(result => !result.Success))
            {
                Log.Error("Failed to {Operation} for schedule {ScheduleId}: {Message}",
                    result.Operation, Model.ScheduleId, result.Message);
                success = false;
            }

            return success;
        }

        protected void HandleCancel()
        {
            var redirectUrl = _isFromBedRequest ? "/administration/manage/bedrequests" : "/administration/manage/schedules";
            _nav.NavigateTo(redirectUrl);
        }

        private void CloseVerificationDialog()
        {
            _showVerificationDialog = false;
        }

        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "3" },
        };
    }
}
