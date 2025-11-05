using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Action = Syncfusion.Blazor.Grids.Action;


namespace BedBrigade.Client.Components.Pages.Administration.Manage
{
    public partial class ManageSchedules : ComponentBase
    {
        // Data Services
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private NavigationManager? _navigationManager { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Parameter] public int? Id { get; set; }
        protected SfGrid<Common.Models.Schedule>? Grid { get; set; }
        protected List<Common.Models.Schedule>? lstSchedules { get; set; }
        protected List<Location>? lstLocations;

        private string userRole = String.Empty;
        private string userName = String.Empty;
        private string userLocationName = String.Empty;
        public List<EventStatusEnumItem>? lstEventStatuses { get; private set; }
        public List<EventTypeEnumItem>? lstEventTypes { get; private set; }
        public DateTime ScheduleStartDate { get; set; }
        public DateTime ScheduleStartTime { get; set; }
        public bool enabledLocationSelector { get; set; } = true;

        private const string EventDate = "EventDateScheduled";
        private const string FutureFilter = "future";
        // Edit Form

        protected List<string>? ToolBar;
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? RecordText { get; set; } = "Loading Schedules ...";
        public bool NoPaging { get; private set; }
        public int selectedScheduleId = 0;

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px", EnableResize = true };

        private User? _currentUser = new User();
        private int _selectedLocationId = 0;
        private List<UsState>? StateList = AddressHelper.GetStateList();
        public string ManageScheduleMessage { get; set; }
        protected List<GridSortColumn> DefaultSortColumns { get; set; } = new List<GridSortColumn> { new GridSortColumn { Field = EventDate, Direction = SortDirection.Ascending } };

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                await LoadUserData();
                await LoadLocations();
                SetupToolbar();
                await LoadScheduleData(FutureFilter);
                lstEventStatuses = EnumHelper.GetEventStatusItems();
                lstEventTypes = EnumHelper.GetEventTypeItems();
                DefaultSortColumns = new List<GridSortColumn> { new GridSortColumn { Field = EventDate, Direction = SortDirection.Ascending } };
                
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ScheduleGrid component");
                _toastService.Error("Error", $"An error occurred while initializing the page: {ex.Message}");
            }
        }


        protected override void OnAfterRender(bool firstRender)
        {
            if (!firstRender)
            {
                if (_svcAuth.UserHasRole(RoleNames.CanManageSchedule))
                {
                    Grid.EditSettings.AllowEditOnDblClick = true;
                    Grid.EditSettings.AllowDeleting = true;
                    Grid.EditSettings.AllowAdding = true;
                    Grid.EditSettings.AllowEditing = true;
                    StateHasChanged();
                }
            }
        }

        private void SetupToolbar()
        {
            if (_svcAuth.UserHasRole(RoleNames.CanManageSchedule))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ManageScheduleMessage = $"Manage Schedules for {userLocationName}";
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ManageScheduleMessage = $"View Schedules for {userLocationName}";
            }
        }
        // Async Init




        private async Task LoadUserData()
        {
            Log.Information($"{_svcAuth.UserName} went to the Manage Schedules Page");

            userRole = _svcAuth.UserRole;

            _currentUser = (await _svcUser!.GetCurrentLoggedInUser()).Data;

            if (_currentUser != null)
            {
                _selectedLocationId = _currentUser.LocationId;
            }
        } // User Data

        private async Task LoadLocations()
        {
            //This GetAllAsync should always have less than 1000 records
            var dataLocations = await _svcLocation!.GetAllAsync();

            if (dataLocations.Success && dataLocations.Data != null) // 
            {
                lstLocations = dataLocations.Data;
                if (lstLocations != null && lstLocations.Count > 0 && _svcAuth.LocationId > 0)
                { // select User Location Name 
                    userLocationName = lstLocations.Find(e => e.LocationId == _svcAuth.LocationId).Name;
                } // Locations found             

            }
        } // Load Locations

        private async Task LoadScheduleData(string filter)
        {
            ServiceResponse<List<Common.Models.Schedule>> result;

            switch (filter)
            {
                case FutureFilter:
                    result = await _svcSchedule.GetFutureSchedulesByLocationId(_selectedLocationId);
                    break;
                case "past":
                    result = await _svcSchedule.GetPastSchedulesByLocationId(_selectedLocationId);
                    break;
                default:
                    result = await _svcSchedule.GetSchedulesByLocationId(_selectedLocationId);
                    break;
            }

            if (result.Success && result.Data != null)
            {
                lstSchedules = result!.Data;
            }
            else
            {
                lstSchedules = new List<Common.Models.Schedule>();
                Log.Error("Error loading future schedules: {ErrorMessage}", result.Message);
                _toastService.Error("Error", $"An error occurred while loading future schedules: {result.Message}");
            }
        }


        protected async Task OnLoad()
        {
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Schedule };
            var result = await _svcUserPersist.GetGridPersistence(persist);
            if (result.Success && result.Data != null)
            {
                await Grid.SetPersistDataAsync(result.Data);
            }
        }

        protected async Task OnDestroyed()
        {
            await SaveGridPersistence();
        }

        private async Task SaveGridPersistence()
        {
            _state = await Grid.GetPersistData();
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Schedule, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Schedule} : {result.Message}");
            }
        }

        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
                CurrentFilterOption = FutureFilter;
                await LoadScheduleData(CurrentFilterOption);
                await Grid.ResetPersistDataAsync();
                await SaveGridPersistence();
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

        public async Task OnActionBegin(Syncfusion.Blazor.Grids.ActionEventArgs<Common.Models.Schedule> args)
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
                    args.Cancel = true;
                    Add();
                    break;

                case Action.BeginEdit:
                    await Edit(args);
                    break;
            }

        }

        private void Add()
        {
            _navigationManager.NavigateTo($"/administration/admintasks/addeditschedule/{_selectedLocationId}");
        }

        protected async Task Edit(ActionEventArgs<Common.Models.Schedule> args)
        {
            var schedule = args.Data;
            args.Cancel = true;
            _navigationManager.NavigateTo($"/administration/admintasks/addeditschedule/{_selectedLocationId}/{schedule.ScheduleId}");
        }

        private async Task Delete(Syncfusion.Blazor.Grids.ActionEventArgs<Common.Models.Schedule> args)
        {
            try
            {
                List<Common.Models.Schedule> records = await Grid.GetSelectedRecordsAsync();
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
            if (Grid != null)
            {
                PdfExportProperties exportProperties = new PdfExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("Schedules", ".pdf"),
                    PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
                };
                await Grid.ExportToPdfAsync(exportProperties);
            }
        }
        protected async Task ExcelExport()
        {
            if (Grid != null)
            {
                ExcelExportProperties exportProperties = new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("Schedules", ".xlsx"),
                };

                await Grid.ExportToExcelAsync(exportProperties);
            }
        }
        protected async Task CsvExportAsync()
        {
            if (Grid != null)
            {
                ExcelExportProperties exportProperties = new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("Schedules", ".csv"),
                };

                await Grid.ExportToCsvAsync(exportProperties);
            }
        }

        public void RowSelectHandler(RowSelectEventArgs<Common.Models.Schedule> args)
        {
            selectedScheduleId = args.Data.ScheduleId;
            _selectedLocationId = args.Data.LocationId;
        }

        public class GridFilterOption
        {
            public string ID { get; set; }
            public string Text { get; set; }
        }

        public string CurrentFilterOption = "future";

        private List<GridFilterOption> GridFilterOptions = new List<GridFilterOption>
        {
            new GridFilterOption() { ID = FutureFilter, Text = "In the Future" },
            new GridFilterOption() { ID = "past", Text = "In the Past" },
            new GridFilterOption() { ID = "all", Text = "All Schedules" },
        };

        private async Task OnFilterChange(ChangeEventArgs<string, GridFilterOption> args)
        {
            await LoadScheduleData(args.Value);
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