using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using Action = Syncfusion.Blazor.Grids.Action;
using Serilog;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components.Pages.Administration.Manage
{
    public partial class ManageVolunteers : ComponentBase
    {
        [Inject] private IVolunteerDataService? _svcVolunteer { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private NavigationManager? _navigationManager { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        protected List<Volunteer>? Volunteers { get; set; }
        public List<VehicleTypeEnumItem>? lstVehicleTypes { get; private set; }
        protected List<Location>? Locations { get; private set; }
        protected SfGrid<Volunteer>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;


        private string userRole = String.Empty;
        private string userName = String.Empty;
        private string userLocationName = String.Empty;
        private int userLocationId = 0;
        private string ErrorMessage = String.Empty;
        protected string? _state { get; set; }


        protected string? RecordText { get; set; } = "Loading Volunteers ...";
        public bool NoPaging { get; private set; }
        private bool ShouldDisplayEmailMessage = false;
        public string ManageVolunteersMessage { get; set; }
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await LoadUserData();
                await LoadLocations();
                await LoadVolunteerData();
                SetupToolbar();
                lstVehicleTypes = EnumHelper.GetVehicleTypeItems();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ManageVolunteers component");
                ErrorMessage = "An error occurred while initializing the page: " + ex.Message;
            }
        }

        private void SetupToolbar()
        {
            if (_svcAuth.UserHasRole(RoleNames.CanManageVolunteers))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
                ManageVolunteersMessage = $"Manage Volunteers for {userLocationName}";
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
                ManageVolunteersMessage = $"View Volunteers for {userLocationName}";
            }
        }


        private async Task LoadUserData()
        {
            userLocationId = _svcAuth.LocationId;
            userName = _svcAuth.UserName;
            Log.Information($"{userName} went to the Manage Volunteers Page");
            userRole = _svcAuth.UserRole;
        } // User Data


        private async Task LoadLocations()
        {
            //This GetAllAsync should always have less than 1000 records
            var dataLocations = await _svcLocation!.GetAllAsync();

            if (dataLocations.Success) // 
            {
                Locations = dataLocations.Data;
                if (Locations != null && Locations.Count > 0 && userLocationId > 0)
                { // select User Location Name 
                    userLocationName = Locations.Find(e => e.LocationId == userLocationId).Name;
                } // Locations found             

            }
        } // Load Locations

        private async Task LoadVolunteerData()
        {
            try // get Volunteer List ===========================================================================================
            {
                var dataVolunteer = await _svcVolunteer.GetAllForLocationAsync(_svcAuth.LocationId); // get Schedules
                Volunteers = new List<Volunteer>();

                if (dataVolunteer.Success && dataVolunteer != null)
                {

                    if (dataVolunteer.Data.Count > 0)
                    {
                        Volunteers = dataVolunteer.Data.ToList(); // retrieve existing media records to temp list
                    }
                    else
                    {
                        ErrorMessage = "No Volunteers Data Found";
                    } // no rows in Media


                } // the first success
            }
            catch (Exception ex)
            {
                ErrorMessage = "No DB Files. " + ex.Message;
            }

        } // OnInit


        /// <summary>
        /// On loading of the Grid get the user grid persisted data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Volunteer };
            var result = await _svcUserPersist.GetGridPersistence(persist);
            if (result.Success && result.Data != null)
            {
                await Grid.SetPersistDataAsync(result.Data);
            }
        }


        /// <summary>
        /// On destroying of the grid save its current state
        /// </summary>
        /// <returns></returns>
        protected async Task OnDestroyed()
        {
            await SaveGridPersistence();
        }

        private async Task SaveGridPersistence()
        {
            _state = await Grid.GetPersistData();
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Volunteer, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Volunteer} : {result.Message}");
            }
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistDataAsync();
                await SaveGridPersistence();
            }
            else if (args.Item.Text == "Pdf Export")
            {
                await PdfExport();
            }
            else if (args.Item.Text == "Excel Export")
            {
                await ExcelExport();
            }
            else if (args.Item.Text == "Csv Export")
            {
                await CsvExportAsync();
            }
        }

        protected async Task Edit(ActionEventArgs<Volunteer> args)
        {
            var volunteer = (await Grid.GetSelectedRecordsAsync()).FirstOrDefault();

            if (volunteer != null)
            {
                args.Cancel = true; 
                _navigationManager.NavigateTo($"/administration/admintasks/addeditvolunteer/{userLocationId}/{volunteer.VolunteerId}");
            }
        }

        public async Task OnActionBegin(ActionEventArgs<Volunteer> args)
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

        private async Task Delete(ActionEventArgs<Volunteer> args)
        {
            List<Volunteer> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcVolunteer.DeleteAsync(rec.VolunteerId);

                if (deleteResult.Success)
                {
                    _toastService.Success("Success", $"Volunteer {rec.FirstName} {rec.LastName} deleted successfully.");
                }
                else
                {
                    Log.Error($"Failed to delete Volunteer {rec.FirstName} {rec.LastName}: {deleteResult.Message}");
                    _toastService.Error("Error", $"Failed to delete Volunteer {rec.FirstName} {rec.LastName}: {deleteResult.Message}");
                    args.Cancel = true;
                }
            }
        }

        private void Add()
        {
            _navigationManager.NavigateTo($"/administration/admintasks/addeditvolunteer/{userLocationId}");
        }

        protected void DataBound()
        {
            if (Volunteers.Count == 0) RecordText = "No Volunteer records found";
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
                    FileName = FileUtil.BuildFileNameWithDate("Volunteers", ".pdf"),
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
                    FileName = FileUtil.BuildFileNameWithDate("Volunteers", ".xlsx"),
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
                    FileName = FileUtil.BuildFileNameWithDate("Volunteers", ".csv"),
                };

                await Grid.ExportToCsvAsync(exportProperties);
            }
        }



    }
}
