using BedBrigade.Common.Logic;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using BedBrigade.Common.Models;
using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using Syncfusion.Blazor.Schedule;
using static BedBrigade.Common.Logic.AddressHelper;
using System.Text;
using System.IO;
using BedBrigade.SpeakIt;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Components.Pages
{

    public partial class Calendar : ComponentBase
    {
        [Inject] private ILocationDataService? _svcLocation { get; set; }     
        [Inject] private NavigationManager? _navigationManager { get; set; }

        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject]  private ILocationState? _locationState { get; set; }
        [Inject] private IContentTranslationDataService? _svcContentTranslation { get; set; }

        [Inject] private IJSRuntime _js { get; set; }
   
        [Inject] private ILanguageService _svcLanguage { get; set; }

        [Inject] private ITranslationDataService? _translateLogic { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ToastService _toastService { get; set; }

        [Parameter] public string? LocationRoute { get; set; }
      

        public View CurrentView { get; set; } = View.Month;
        private int CurrentYear;
        private ServiceResponse<List<Schedule>>? recordResult;

        public DateTime CurrentDate { get; private set; }

        // private DateTime CurrentDate { get; set; }

        protected List<Schedule>? lstSchedules { get; set; }
        protected List<Location>? lstLocations;
        private List<AppointmentData> dataSource = new List<AppointmentData>();
        private string ErrorMessage = String.Empty;
        private bool IsDisplayCalendar = false;
        private MarkupString EventsAlert = BootstrapHelper.GetBootstrapMessage("warning", "Sorry, there are no scheduled volunteer events in the selected location.<br/>Please select another location or try again later.", "", false);

        protected override async Task OnInitializedAsync()
        {
            
            CurrentYear = DateTime.Today.Year;
            CurrentDate = DateTime.Today;

            if (LocationRoute != null && LocationRoute.Length > 0)
            {
                var locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");

                if (locationResponse!=null && locationResponse.Success)
                {                   

                    _locationState.Location = LocationRoute;
                    recordResult = await _svcSchedule.GetAvailableSchedulesByLocationId(locationResponse.Data.LocationId);
                }
                else
                {
                    _navigationManager.NavigateTo($"/Sorry/{LocationRoute}/calendar");
                }

            }
            else // all events
            {
                recordResult = await _svcSchedule.GetAllAsync();
            }                     

            try
            {            
                if (recordResult!=null && recordResult.Success)
                {
                    lstSchedules = recordResult!.Data;
                    if (lstSchedules!=null && lstSchedules.Count > 0)
                    {
                        IsDisplayCalendar = true;
                        dataSource = GetCalendarAppointments(lstSchedules);
                    }
                    //Debug.WriteLine("Loaded events: " + lstSchedules.Count.ToString());
                 
                }
                else
                {
                    //ErrorMessage = "Could not retrieve schedule. " + recordResult.Message;
                    _toastService.Error("Error", recordResult.Message);
                    return;
                }
            }
            catch (Exception ex)
            {
                _toastService.Error("Error", ex.Message);
                return;
            }                     
            
        } // Init

        private async Task LoadScheduleData()
        {

            ServiceResponse<List<Schedule>> recordResult;

            try
            {
               
                 recordResult = await _svcSchedule.GetAllAsync();
               

                if (recordResult.Success)
                {
                    lstSchedules = recordResult!.Data;
                    Debug.WriteLine("Loaded events: " + lstSchedules.Count.ToString());
                    //DataSource = await TransferEventToAppointment();
                }
                else
                {
                    ErrorMessage = "Could not retrieve schedule. " + recordResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not retrieve schedule. " + ex.Message;
            }


        }

        private async Task<Location> GetLocation(int LocationId)
        {
            Location currentLocation = new Location();

            try
            {

                var myLocation = await _svcLocation.GetByIdAsync(LocationId);

                if (myLocation.Success && myLocation.Data != null)
                {
                    currentLocation = myLocation.Data;

                }



            }

            catch (Exception ex) { }

            return currentLocation;

        } // Get Location

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
