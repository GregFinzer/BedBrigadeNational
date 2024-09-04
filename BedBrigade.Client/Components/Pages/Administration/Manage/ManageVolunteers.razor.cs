using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
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
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private NavigationManager? _navigationManager { get; set; }
        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
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
        public bool isLocationAdmin = false;
        private string ErrorMessage = String.Empty;
        protected string? _state { get; set; }
        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; } = 3000;

        protected string? RecordText { get; set; } = "Loading Volunteers ...";
        public bool NoPaging { get; private set; }
        private bool ShouldDisplayEmailMessage = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadUserData();
            await LoadLocations();
            await LoadVolunteerData();
            lstVehicleTypes = EnumHelper.GetVehicleTypeItems();

        } // Async Init


        private async Task LoadUserData()
        {
            var authState = await _authState!.GetAuthenticationStateAsync();
            Identity = authState.User;

            userLocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);

            userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Manage Volunteers Page");

            if (Identity.IsInRole(RoleNames.NationalAdmin) || Identity.IsInRole(RoleNames.LocationAdmin))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }


            if (Identity.IsInRole(RoleNames.NationalAdmin)) // not perfect! for initial testing
            {
                userRole = RoleNames.NationalAdmin;
                isLocationAdmin = false;
            }
            else // Location User
            {
                if (Identity.IsInRole(RoleNames.LocationAdmin))
                {
                    userRole = RoleNames.LocationAdmin;
                    isLocationAdmin = true;
                }

                if (Identity.IsInRole(RoleNames.LocationAuthor))
                {
                    userRole = RoleNames.LocationAuthor;
                    isLocationAdmin = true;
                }



            } // Get User Data
        } // User Data


        private async Task LoadLocations()
        {
            var dataLocations = await _svcLocation!.GetAllAsync();

            if (dataLocations.Success) // 
            {
                Locations = dataLocations.Data;
                if (Locations != null && Locations.Count > 0)
                { // select User Location Name 
                    userLocationName = Locations.Find(e => e.LocationId == userLocationId).Name;
                } // Locations found             

            }
        } // Load Locations

        private async Task LoadVolunteerData()
        {
            try // get Volunteer List ===========================================================================================
            {
                var dataVolunteer = await _svcVolunteer.GetAllAsync(); // get Schedules
                Volunteers = new List<Volunteer>();

                if (dataVolunteer.Success && dataVolunteer != null)
                {

                    if (dataVolunteer.Data.Count > 0)
                    {
                        Volunteers = dataVolunteer.Data.ToList(); // retrieve existing media records to temp list

                        // Location Filter
                        if (isLocationAdmin)
                        {
                            Volunteers = Volunteers.FindAll(e => e.LocationId == userLocationId); // Location Filter
                        }

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
            string userName = await _svcUser.GetUserName();
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
            string userName = await _svcUser.GetUserName();
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
                ToastTitle = "Delete Volunteer";
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    ToastContent = $"Unable to Delete. Volunteer is in use.";
                    args.Cancel = true;
                }
                ToastTimeout = 4000;
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

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
            PdfExportProperties ExportProperties = new PdfExportProperties
            {
                FileName = "Volunteer" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Volunteer " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExportToPdfAsync();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Volunteer " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.ExportToCsvAsync(ExportProperties);
        }



    }
}
