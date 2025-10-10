using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

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

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Permissions
                CanSetLocation = _svcUser.IsUserNationalAdmin();

                await LoadLocations();
                await LoadModel();

                // Enum lists
                EventStatuses = EnumHelper.GetEventStatusItems();
                EventTypes = EnumHelper.GetEventTypeItems();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing AddEditSchedule component");
                _toast.Error("Error", "An error occurred while loading the schedule data.");
            }
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
            if (ScheduleId.HasValue)
            {
                var result = await _svcSchedule.GetByIdAsync(ScheduleId.Value);
                if (result.Success && result.Data != null)
                {
                    Model = result.Data;
                    // Split date/time for editors
                    ScheduleStartDate = Model.EventDateScheduled.Date;
                    ScheduleStartTime = new DateTime(Model.EventDateScheduled.TimeOfDay.Ticks);
                }
                else
                {
                    ErrorMessage = result.Message ?? "Could not load schedule.";
                }
            }
            else
            {
                // Add mode
                Model = new Common.Models.Schedule();
                Model.LocationId = LocationId ?? _svcAuth.LocationId;
            }
        }

        private DateTime ComposeEventDate()
        {
            // Combine date and time string (HH:mm)
            if (!TimeSpan.TryParse(ScheduleTimeString, out var ts))
            {
                ts = new TimeSpan(8, 0, 0);
            }
            return ScheduleDate.Date.Add(ts);
        }

        protected async Task HandleValidSubmit()
        {
            Model.EventDateScheduled = ComposeEventDate();

            if (ScheduleId.HasValue)
            {
                var update = await _svcSchedule.UpdateAsync(Model);
                if (update.Success)
                {
                    _toast.Success("Success", "Schedule updated successfully");
                    _nav.NavigateTo("/administration/manage/schedules");
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
                _nav.NavigateTo("/administration/manage/schedules");
            }
            else
            {
                ErrorMessage = create.Message;
                _toast.Error("Error", create.Message);
            }
        }

        protected void HandleCancel()
        {
            _nav.NavigateTo("/administration/manage/schedules");
        }

        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "3" },
        };
    }
}
