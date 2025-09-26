using BedBrigade.Common.Logic;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using Syncfusion.Blazor.Schedule;
using System.Text;
using Serilog;
using StringUtil = BedBrigade.Common.Logic.StringUtil;

namespace BedBrigade.Client.Components.Pages
{

    public partial class Calendar : ComponentBase
    {
        [Inject] private ILocationDataService? _svcLocation { get; set; }     
        [Inject] private NavigationManager? _navigationManager { get; set; }

        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject]  private ILocationState? _locationState { get; set; }

        [Inject] private ToastService _toastService { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Parameter] public string? LocationRoute { get; set; }
      

        public View CurrentView { get; set; } = View.Month;
        private ServiceResponse<List<Schedule>>? scheduleResult;

        public DateTime CurrentDate { get; private set; }

        protected List<Schedule>? lstSchedules { get; set; }
        private List<AppointmentData> dataSource = new List<AppointmentData>();
        private bool IsDisplayCalendar = false;
        private MarkupString EventsAlert = BootstrapHelper.GetBootstrapMessage("warning", "Sorry, there are no scheduled volunteer events in the selected location.<br/>Please select another location or try again later.", "", false);

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                CurrentDate = DateTime.Today;

                var locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");

                if (locationResponse.Success && locationResponse.Data != null)
                {
                    _locationState.Location = LocationRoute;
                    scheduleResult = await _svcSchedule.GetAvailableSchedulesByLocationId(locationResponse.Data.LocationId);

                    if (scheduleResult.Success && scheduleResult.Data != null)
                    {
                        lstSchedules = scheduleResult.Data;
                        IsDisplayCalendar = true;
                        dataSource = GetCalendarAppointments(lstSchedules);
                    }
                    else
                    {
                        Log.Error("Calendar OnInitializedAsync " + scheduleResult.Message);
                        _toastService.Error("Error", scheduleResult.Message);
                    }
                }
                else
                {
                    _navigationManager.NavigateTo($"/Sorry/{LocationRoute}/calendar");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Calendar OnInitializedAsync");
                _toastService.Error("Error", ex.Message);
            }
        } 


        private List<AppointmentData> GetCalendarAppointments(List<Schedule> BBEvents)
        {
            List<AppointmentData> appointments = new();
            // Transfer data to destination list
            appointments = BBEvents
                .Select(s => new AppointmentData
                {
                    Id = s.ScheduleId,
                    Subject = StringUtil.IsNull(s.EventName,"'"),
                    StartTime = s.EventDateScheduled, 
                    EndTime = s.EventDateScheduled.AddHours(s.EventDurationHours),
                    Description = StringUtil.IsNull(s.EventNote,""),
                    Location = FormatAddress(s.Address, s.City, s.State, s.PostalCode),
                    GroupName = StringUtil.IsNull(s.GroupName, ""),
                    OrganizerName = s.OrganizerName,
                    OrganizerPhone = s.OrganizerPhone,
                    OrganizerEmail = s.OrganizerEmail,
                    Volunteers = FormatVolunteerString(s.VolunteersRegistered, s.VolunteersMax)
                })
                .ToList();

            return appointments;
        } // Convert

        public string FormatVolunteerString(int VolunteersRegistered, int VolunteersMax)
        {
            StringBuilder volunteersString = new StringBuilder();
                       
            if(VolunteersMax > 0)
            {
                volunteersString.Append(VolunteersRegistered.ToString());
                volunteersString.Append("/");
                volunteersString.Append(VolunteersMax.ToString());
            }

            return volunteersString.ToString().Trim(); 

        } // Volunteers String


        public string FormatAddress(String? Street, String? City, String? State, String? ZipCode)
        {
            StringBuilder addressString = new StringBuilder();

            // Build the address part (US format)
            if (!string.IsNullOrWhiteSpace(Street))
                addressString.AppendLine(Street);

            if (!string.IsNullOrWhiteSpace(City) || !string.IsNullOrWhiteSpace(State) || !string.IsNullOrWhiteSpace(ZipCode))
            {
                if (!string.IsNullOrWhiteSpace(City))
                {
                    addressString.Append(", ");
                    addressString.Append(City);
                }

                if (!string.IsNullOrWhiteSpace(State))
                {
                    if (addressString.Length > 0)
                        addressString.Append(", ");
                        addressString.Append(State);
                }

                if (!string.IsNullOrWhiteSpace(ZipCode))
                {
                    if (addressString.Length > 0)
                        addressString.Append(" ");
                    addressString.Append(ZipCode);
                }

                addressString.AppendLine(); // New line after address
            }

            return addressString.ToString().Trim();

        } // Address Format
    


    } // page class


} // namespace
