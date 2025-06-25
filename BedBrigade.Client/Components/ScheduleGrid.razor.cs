using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Action = Syncfusion.Blazor.Grids.Action;
using Syncfusion.Blazor.DropDowns;
using BedBrigade.Data.Services;
using Serilog;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;


namespace BedBrigade.Client.Components
{
    public partial class ScheduleGrid : ComponentBase
    {
        // Data Services
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        protected SfGrid<Schedule>? Grid { get; set; }
        protected List<Schedule>? lstSchedules { get; set; }
        protected List<Location>? lstLocations;

        private string userRole = String.Empty;
        private string userName = String.Empty;
        private string userLocationName = String.Empty;
        public bool isLocationAdmin = false;
        public List<EventStatusEnumItem>? lstEventStatuses { get; private set; }
        public List<EventTypeEnumItem>? lstEventTypes { get; private set; }
        public DateTime ScheduleStartDate { get; set; }
        public DateTime ScheduleStartTime { get; set; }
        public bool enabledLocationSelector { get; set; } = true;

        private const string EventDate = "EventDateScheduled";
        // Edit Form

        protected List<string>? ToolBar;
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? RecordText { get; set; } = "Loading Schedules ...";
        public bool NoPaging { get; private set; }
        public int selectedScheduleId = 0;

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight="200px", EnableResize=true };

        private User? _currentUser = new User();
        private int _selectedLocationId = 0;
        private List<UsState>? StateList = AddressHelper.GetStateList();


        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                await LoadUserData();
                await LoadLocations();
                await LoadScheduleData();
                lstEventStatuses = EnumHelper.GetEventStatusItems();
                lstEventTypes = EnumHelper.GetEventTypeItems();
                await SetInitialFilter();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ScheduleGrid component");
                _toastService.Error("Error", $"An error occurred while initializing the page: {ex.Message}");
            }


        } // Async Init

        private async Task SetInitialFilter()
        {
            if (Grid != null)
            {
                if (lstSchedules != null && lstSchedules.Count > 0 && Grid != null)
                {
                    Grid.SelectedRowIndex = 0;
                }

                await Grid.FilterByColumnAsync(EventDate, "greaterthanorequal",
                    DateTime.Today); // default grid filter: future events
            }
        }


        private async Task LoadUserData()
        {
            Log.Information($"{_svcAuth.UserName} went to the Manage Schedules Page");

            if (_svcAuth.UserHasRole(RoleNames.CanManageSchedule))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
            }

            if (_svcAuth.IsNationalAdmin) 
            {
                userRole = RoleNames.NationalAdmin;
            }
            else // Location User
            {
                if (_svcAuth.UserHasRole(RoleNames.LocationAdmin))
                {
                    userRole = RoleNames.LocationAdmin;
                    isLocationAdmin = true;
                }

                if (_svcAuth.UserHasRole(RoleNames.LocationAuthor))
                {
                    userRole = RoleNames.LocationAuthor;
                    isLocationAdmin = true;
                }
                if (_svcAuth.UserHasRole(RoleNames.LocationScheduler))
                {
                    userRole = RoleNames.LocationScheduler;
                    isLocationAdmin = true;
                }


            } // Get User Data

            _currentUser = (await _svcUser!.GetCurrentLoggedInUser()).Data;
            _selectedLocationId = _currentUser.LocationId;
        } // User Data

        private async Task LoadLocations()
        {
            var dataLocations = await _svcLocation!.GetAllAsync();

            if (dataLocations.Success && dataLocations.Data != null) // 
            {
                lstLocations = dataLocations.Data;
                if (lstLocations != null && lstLocations.Count > 0)
                { // select User Location Name 
                    userLocationName = lstLocations.Find(e => e.LocationId == _selectedLocationId).Name;                    
                } // Locations found             

            }
        } // Load Locations

        private async Task LoadScheduleData()
        {
            ServiceResponse<List<Schedule>> recordResult;
            if (_svcAuth.IsNationalAdmin)
            {
                recordResult = await _svcSchedule.GetAllAsync();
            }
            else
            {
                recordResult = await _svcSchedule.GetSchedulesByLocationId(_selectedLocationId);
            }

            if (recordResult.Success && recordResult.Data != null)
            {
                lstSchedules = recordResult!.Data;
            }
            else
            {
                lstSchedules = new List<Schedule>();
                Log.Error("Error loading schedules: {ErrorMessage}", recordResult.Message);
                _toastService.Error("Error", $"An error occurred while loading schedules: {recordResult.Message}");
            }
        }


        protected async Task OnLoad()
        {
            //TODO: Load the grid state for this grid
            //It is currently not possible to load the grid state for this grid 
            //It has to do with how we are filtering the grid client side
            //After saving the grid state, when it is loaded there are no records shown.
        }

        protected async Task OnDestroyed()
        {
            //TODO: Save the grid state for this grid
            //It is currently not possible to save the grid state for this grid 
            //It has to do with how we are filtering the grid client side probably
            //After saving the grid state, when it is loaded there are no records shown.
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
               await Grid.ResetPersistDataAsync();
               //It is not possible to save the grid state for this grid for some reason
               return;
            }

            if (args.Item.Text == "Pdf Export")
            {
                await PdfExport();
            }
            if (args.Item.Text == "Excel Export")
            {
                await ExcelExport();
                return;
            }
            if (args.Item.Text == "Csv Export")
            {
                await CsvExportAsync();
                return;
            }

        }

        public async Task OnActionBegin(Syncfusion.Blazor.Grids.ActionEventArgs<Schedule> args)
        {
            var requestType = args.RequestType;
            switch (requestType)
            {
                case Action.Searching:
                    RecordText = "Searching ... Record Not Found.";
                    break;

                case Action.Delete:
                    await Delete(args);
                    break;

                case Action.Add:
                    Add();
                    break;

                case Action.Save:
                    await Save(args);
                    break;
                case Action.BeginEdit:
                    BeginEdit();
                    break;
            }

        }

        private async Task Delete(Syncfusion.Blazor.Grids.ActionEventArgs<Schedule> args)
        {
            try
            {
                List<Schedule> records = await Grid.GetSelectedRecordsAsync();
                foreach (var rec in records)
                {
                    var deleteResult = await _svcSchedule.DeleteAsync(rec.ScheduleId);
                    if (deleteResult.Success)
                    {
                        _toastService.Success("Delete Schedule",
                            $"Schedule for {rec.EventName} {rec.EventDateScheduled.ToShortDateString()} deleted Successfully");
                    }
                    else
                    {
                        Log.Error("Unable to delete schedule " + deleteResult.Message);
                        _toastService.Error("Delete Schedule", $"Unable to delete Schedule. {deleteResult.Message}");
                        args.Cancel = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting schedule");
                _toastService.Error("Delete Schedule", $"An error occurred while deleting the schedule: {ex.Message}");
                args.Cancel = true;
            }
        }

        private void Add()
        {
            HeaderTitle = _lc.Keys["Add"] + " " + _lc.Keys["Schedule"];
            ButtonTitle = _lc.Keys["Add"] + " " + _lc.Keys["Schedule"]; 
            if (isLocationAdmin)
            {
                enabledLocationSelector = false;
            }
            else
            {
                enabledLocationSelector = true;
            }   

        }

        private async Task Save(Syncfusion.Blazor.Grids.ActionEventArgs<Schedule> args)
        {
            try
            {

                Schedule editSchedule = args.Data;

                editSchedule.EventDateScheduled = ScheduleStartDate.Date + ScheduleStartTime.TimeOfDay;

                if (editSchedule.ScheduleId != 0) // Updated schedule
                {
                    //Update Schedule Record
                    var updateResult = await _svcSchedule.UpdateAsync(editSchedule);

                    if (updateResult.Success)
                    {
                        _toastService.Success("Update Schedule",
                            $"Schedule for {editSchedule.EventDateScheduled.ToShortDateString()} updated Successfully");
                    }
                    else
                    {
                        Log.Error("Unable to update schedule " + updateResult.Message);
                        _toastService.Error("Update Schedule", $"Unable to update Schedule. {updateResult.Message}");
                    }
                }
                else // new schedule
                {
                    var addResult = await _svcSchedule.CreateAsync(editSchedule);
                    if (addResult.Success && addResult.Data != null)
                    {
                        editSchedule = addResult.Data; // added Schedule
                    }

                    if (editSchedule != null && editSchedule.ScheduleId > 0)
                    {
                        _toastService.Success("Add Schedule",
                            $"New Schedule for {editSchedule.EventDateScheduled.ToShortDateString()} added Successfully");
                    }
                    else
                    {
                        Log.Error("Unable to add schedule " + addResult.Message);
                        _toastService.Error("Add Schedule", $"Unable to add Schedule. {addResult.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving schedule");
                _toastService.Error("Save Schedule", $"An error occurred while saving the schedule: {ex.Message}");
            }

            await Grid.Refresh();
        }

        private void BeginEdit()
        {          
            HeaderTitle = _lc.Keys["Update"] + " " + _lc.Keys["Schedule"]+ " #" + selectedScheduleId.ToString();
            ButtonTitle = _lc.Keys["Update"];
            enabledLocationSelector = false;          
        }

        public async Task Save(Schedule schedule)        
        {          
            await Grid.EndEditAsync();
        }

        public async Task Cancel()
        {
            await Grid.CloseEditAsync();
        }
        protected void DataBound()
        {
            if (lstSchedules.Count == 0) RecordText = "No Schedule records found";
            if (Grid.TotalItemCount <= Grid.PageSettings.PageSize)  //compare total grid data count with pagesize value 
            {
                NoPaging = true;
            }
            else
                NoPaging = false;

        }


        protected async Task PdfExport()
        {
            PdfExportProperties ExportProperties = new PdfExportProperties
            {
                FileName = "Schedule" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Schedule" + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExportToExcelAsync();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Schedule" + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.ExportToCsvAsync(ExportProperties);
        }

        public void RowSelectHandler(RowSelectEventArgs<Schedule> args)
        {
            selectedScheduleId = args.Data.ScheduleId;
            _selectedLocationId = args.Data.LocationId;
        }

        public class GridFilterOption
        {
            public string ID { get; set; }
            public string Text { get; set; }
        }

        public string DefaultFilter = "future";

        private List<GridFilterOption> GridDefaultFilter = new List<GridFilterOption>
        {
            new GridFilterOption() { ID = "future", Text = "In the Future" },
            new GridFilterOption() { ID = "past", Text = "In the Past" },
            new GridFilterOption() { ID = "all", Text = "All Schedules" },
        };

        private async Task OnFilterChange(ChangeEventArgs<string, GridFilterOption> args)
        {   // External Grid Filtering by Event Date
            //Debug.WriteLine("The Grid Filter DropDownList Value", args.Value);

            switch (args.Value)
            {
                case "future":
                    await Grid.FilterByColumnAsync(EventDate, "greaterthanorequal", DateTime.Today);
                    break;
                case "past":
                    await Grid.FilterByColumnAsync(EventDate, "lessthan", DateTime.Today);
                    break;
                default:
                    await Grid.ClearFilteringAsync(EventDate);                  
                    break;

            }
        }

        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "3" },
        };

        Dictionary<string, object> htmlattributeSize = new Dictionary<string, object>()
        {
           { "maxlength", "50" },
        };


    } // class
    


} // namespace