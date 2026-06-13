using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;
using ContentType = BedBrigade.Common.Enums.ContentType;
using System.Diagnostics;


namespace BedBrigade.Client.Components
{
    public partial class BedRequestGrid : ComponentBase
    {
        [Inject] private IBedRequestDataService BedRequestDataService { get; set; } = default!;
        [Inject] private IUserDataService UserDataService { get; set; } = default!;
        [Inject] private IUserPersistDataService UserPersistDataService { get; set; } = default!;
        [Inject] private ILocationDataService LocationDataService { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IMetroAreaDataService MetroAreaDataService { get; set; } = default!;
        [Inject] private IDeliverySheetService DeliverySheetService { get; set; } = default!;
        [Inject] private IContentDataService ContentDataService { get; set; } = default!;
        [Inject] private IConfigurationDataService ConfigurationDataService { get; set; } = default!;
        [Inject] private ITeamSheetService TeamSheetService { get; set; } = default!;

        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private ILanguageContainerService _lc { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IGeoLocationQueueDataService GeoLocationQueueDataService { get; set; } = default!;
        [Parameter] public string? Id { get; set; }

        //private List<UsState>? StateList = AddressHelper.GetStateList();

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";

        protected List<BedRequest>? BedRequests { get; set; }
        protected List<Location>? Locations { get; set; }
        protected List<Location>? metroLocations { get; set; }

        protected Location? UserLocation { get; set; }

        protected SfGrid<BedRequest>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected List<string>? lstPrimaryLanguage;
        protected List<string>? lstSpeakEnglish;

        protected BedRequest BedRequest { get; set; } = new BedRequest();
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }

        //protected bool OnlyRead { get; set; } = false;

        protected string? RecordText { get; set; } = "Loading BedRequests ...";
        public bool NoPaging { get; private set; }
        public string SpeakEnglishVisibility = "hidden";
        public bool IsDialogVisible { get; set; }
        public string DialogHeader { get; set; } = string.Empty;
        public string DialogContent { get; set; } = string.Empty;

        public string ManageBedRequestsMessage { get; set; } = "Manage Bed Requests";

        public List<BedRequestEnumItem>? BedRequestStatuses { get; private set; }

        public string EditPagePath = "/administration/admintasks/addeditbedrequest/";
        public bool IsGeneratingFile { get; set; } // Shows spinner overlay during delivery/team sheet generation

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                                
                if (AuthService != null)
                {
                    Log.Information($"{AuthService.UserName} went to the Manage Bed Requests Page");
                }
                else
                {
                    Log.Information("Unknown user went to the Manage Bed Requests Page");
                }

                SetupToolbar();
                await LoadConfiguration();
                await LoadLocations();
                await LoadUser();                
                await LoadBedRequests();

                BedRequestStatuses = EnumHelper.GetBedRequestStatusItems();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"BedRequestGrid.OnInitializedAsync");
                if (ToastService != null)
                {
                    ToastService.Error("Error", "An error occurred while initializing the Bed Request Grid.");
                }
            }
        }


        private async Task LoadUser()
        {
            var locationId = UserDataService.GetUserLocationId();

            var userLocationResult = await LocationDataService.GetByIdAsync(locationId);
            if (userLocationResult.Success && userLocationResult.Data != null)
            {
                UserLocation = new List<Location> { userLocationResult.Data }.FirstOrDefault(l => l.LocationId == locationId);
                //If this is a metro user, get all contacts for the metro area
                if (UserLocation !=null && UserLocation.IsMetroLocation())
                {
                    await LoadUserMetro();

                }
            }
            else
            {
                 Log.Error($"Unable to load user location for location id {locationId}");
            }
        
        } // Load User Info
        private async Task LoadUserMetro()
        {
            if (UserLocation == null || !UserLocation.IsMetroLocation() || !UserLocation.MetroAreaId.HasValue)
            {
                Log.Error("Cannot idenfify Metro Area for Bed Request Admin User.");
                return;
            }

            var metroAreaResult = await MetroAreaDataService.GetByIdAsync(UserLocation.MetroAreaId.Value);

            if (metroAreaResult.Success && metroAreaResult.Data != null)
            {
                if (AuthService != null && AuthService.UserHasRole(RoleNames.CanManageBedRequests))
                {
                    ManageBedRequestsMessage =
                        $"Manage Bed Requests for the {metroAreaResult.Data.Name} Metro Area";
                }
                else
                {
                    ManageBedRequestsMessage =
                        $"View Bed Requests for the {metroAreaResult.Data.Name} Metro Area";
                }
            }

            var userMetroLocations = await LocationDataService.GetLocationsByMetroAreaId(UserLocation.MetroAreaId.Value);
            if (userMetroLocations.Success && userMetroLocations.Data != null)
            {
                metroLocations = userMetroLocations.Data.ToList();
            }
            else
            {
                Log.Error($"Unable to load metro locations for metro area id {UserLocation.MetroAreaId} : {userMetroLocations.Message}");
            }
        }

        private async Task LoadBedRequests()
        {
            if (metroLocations != null)
            {
                var metroAreaLocationIds = metroLocations.Select(l => l.LocationId).ToList();
                var metroAreaBedRequestResult = await BedRequestDataService.GetAllForLocationList(metroAreaLocationIds);
                if (metroAreaBedRequestResult.Success && metroAreaBedRequestResult.Data != null)
                {
                    BedRequests = metroAreaBedRequestResult.Data.ToList();
                    return;
                }
            }

            if (UserLocation != null && BedRequestDataService != null)
            {
                var locationResult = await BedRequestDataService.GetAllForLocationAsync(UserLocation.LocationId);
                if (locationResult.Success && locationResult.Data != null)
                {
                    BedRequests = locationResult.Data.ToList();
                    if (AuthService != null && AuthService.UserHasRole(RoleNames.CanManageBedRequests))
                    {
                        ManageBedRequestsMessage = $"Manage Bed Requests for {UserLocation.Name}";
                    }
                    else
                    {
                        ManageBedRequestsMessage = $"View Bed Requests for {UserLocation.Name}";
                    }
                }
            }

        } // LoadBedRequests

        private async Task LoadLocations()
        {
            var locationResult = await LocationDataService.GetActiveLocations();
            if (locationResult.Success && locationResult.Data != null)
            {
                Locations = locationResult.Data.ToList();
                var item = Locations.SingleOrDefault(r => r.LocationId == Defaults.NationalLocationId);
                if (item != null)
                {
                    Locations.Remove(item);
                }
            }
        }

        private async Task LoadConfiguration()
        {
            lstPrimaryLanguage = await ConfigurationDataService.GetPrimaryLanguages();
            lstSpeakEnglish = await ConfigurationDataService.GetSpeakEnglish();
        }

        private void SetupToolbar()
        {
            if (AuthService.UserHasRole(RoleNames.CanManageBedRequests))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset", "Delivery Sheet", "Team Sheet", "Sort Waiting Closest" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset", "Delivery Sheet", "Team Sheet" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" };
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (AuthService != null && AuthService.UserHasRole(RoleNames.CanManageBedRequests))
                {
                    Grid.EditSettings.AllowEditOnDblClick = true;
                    Grid.EditSettings.AllowDeleting = true;
                    Grid.EditSettings.AllowAdding = true;
                    Grid.EditSettings.AllowEditing = true;
                    StateHasChanged();
                }
            }

            return base.OnAfterRenderAsync(firstRender);
        }

        /// <summary>
        /// On loading of the Grid get the user grid persisted data
        /// /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            string userName = UserDataService.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.BedRequest };
            var result = await UserPersistDataService.GetGridPersistence(persist);
            if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
            {
                if (Grid != null)
                {
                    await Grid.SetPersistDataAsync(result.Data);
                }
            }
            else
            {
                await FilterWaiting();
            }
        }

        private async Task FilterWaiting()
        {
            if (Grid != null)
            {
                await Grid.FilterByColumnAsync(
                nameof(BedRequest.StatusString), // Column field name
                "equal",                        // Filter operator
                "Waiting"                     // Filter value
            );
            }
        }

        /// <summary>
        /// On destroying of the grid save its current state
        /// /// </summary>
        /// <returns></returns>
        protected async Task OnDestroyed()
        {
            await SaveGridPersistence();
        }

        private async Task SaveGridPersistence()
        {
            string? state = _state;
            if (Grid != null)
            {
                state = await Grid.GetPersistDataAsync();
            }

            if (string.IsNullOrWhiteSpace(state))
            {
                return;
            }

            _state = state;
            string userName = UserDataService.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.BedRequest, Data = _state };
            var result = await UserPersistDataService.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.BedRequest} : {result.Message}");
            }
        }

        private async Task SaveGridPersistenceForNavigationAsync()
        {
            try
            {
                await SaveGridPersistence();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to save BedRequest grid state before navigation.");
            }
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            switch (args.Item.Text)
            {
                case "Reset":
                    if (Grid != null)
                    {
                        await Grid.ResetPersistDataAsync();
                    }
                    await FilterWaiting();
                    await SaveGridPersistence();
                    break;
                case "Pdf Export":
                    await PdfExport();
                    break;
                case "Excel Export":
                    await ExcelExport();
                    break;
                case "Csv Export":
                    await CsvExportAsync();
                    break;
                case "Delivery Sheet":
                    DownloadDeliverySheet();
                    break;
                case "Team Sheet":
                    DownloadTeamSheet();
                    break;
                case "Sort Waiting Closest":
                    await SortClosest();
                    break;
            }
        }

        private async Task SortClosest()
        {
            List<BedRequest> selectedBedRequests = new List<BedRequest>();

            if (Grid != null)
            {
                selectedBedRequests = await Grid.GetSelectedRecordsAsync();

                if (!selectedBedRequests.Any())
                {
                    DialogHeader = "Select Row";
                    DialogContent = "Please select an address row you would like to sort closest.";
                    IsDialogVisible = true;
                    return;
                }
            }

            // Fix for CS8604: Ensure BedRequests is not null before passing to SortBedRequestClosestToAddress
            if (BedRequests != null && BedRequestDataService != null && Grid != null)
            {
                BedRequests = BedRequestDataService.SortBedRequestClosestToAddress(BedRequests, selectedBedRequests.First().BedRequestId);
                await Grid.Refresh();

                // Clear existing sorts before applying new ones
                await Grid.ClearSortingAsync();

                // Sort first by Distance, then by CreateDate
                await Grid.SortColumnsAsync(new List<SortColumn>
                {
                    new SortColumn { Field = "Distance", Direction = Syncfusion.Blazor.Grids.SortDirection.Ascending },
                    new SortColumn { Field = "CreateDate", Direction = Syncfusion.Blazor.Grids.SortDirection.Ascending }
                });
            }
        }

        public async Task OnActionBegin(ActionEventArgs<BedRequest> args)
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
                    // navigate to Add page
                    await NavigateToAdd();
                    args.Cancel = true;
                    break;

                case Action.Save:
                    // Save is handled in Add/Edit page now
                    args.Cancel = true;
                    break;
                case Action.BeginEdit:
                    // For edit navigate to the edit page for the selected record
                    await NavigateToEdit(args);
                    args.Cancel = true;
                    break;
            }

        }

        private async Task Delete(ActionEventArgs<BedRequest> args)
        {
            if (BedRequestDataService != null && Grid != null)
            {
                List<BedRequest> records = await Grid.GetSelectedRecordsAsync();
                foreach (var rec in records)
                {
                    var deleteResult = await BedRequestDataService.DeleteAsync(rec.BedRequestId);

                    if (deleteResult.Success)
                    {
                        if (ToastService != null)
                        {
                            ToastService.Success("Delete Successful", "The delete was successful");
                        }
                    }
                    else
                    {
                        Log.Error($"Unable to delete BedRequest {rec.BedRequestId} : {deleteResult.Message}");
                        if (ToastService != null)
                        {
                            ToastService.Error("Delete Unsuccessful", "The delete was unsucessful");
                        }
                        args.Cancel = true;
                    }
                }
            }

        }

        private void Add()
        {
            if(_lc == null || AuthService == null || Locations == null)
            {
                Log.Error("One or more required services or data are not available for Add Bed Request operation.");
                return;
            }

            HeaderTitle = @_lc.Keys["Add"] + " " + @_lc.Keys["BedRequest"];
            ButtonTitle = @_lc.Keys["Add"] + " " + @_lc.Keys["BedRequest"];
            BedRequest.LocationId = AuthService.LocationId;
            BedRequest.PrimaryLanguage = "English";

            var location = Locations.FirstOrDefault(o => o.LocationId == AuthService.LocationId);

            if (location != null)
            {
                BedRequest.Group = location.Group;
            }
        }

        private async Task NavigateToAdd()
        {
            await SaveGridPersistenceForNavigationAsync();
            int loc = AuthService.LocationId;
            Nav.NavigateTo($"{EditPagePath}{loc}");
        }

        private async Task NavigateToEdit(ActionEventArgs<BedRequest> args)
        {
            // if args.Data is set, use that; otherwise get selected records
            int id = 0;
            if (args.Data != null && args.Data.BedRequestId > 0)
            {
                id = args.Data.BedRequestId;
            }
            else
            {
                if (Grid == null)
                {
                    Log.Error("Bed Request Grid is not initialized.");
                    return;
                }
                var selected = await Grid.GetSelectedRecordsAsync();
                if (selected.Any())
                {
                    id = selected.First().BedRequestId;
                }
            }

            if (id == 0)
            {
               DialogHeader = "Select Row";
               DialogContent = "Please select a row to edit.";
               IsDialogVisible = true;
                return;
            }

            await SaveGridPersistenceForNavigationAsync();
            int loc = AuthService.LocationId;
            Nav.NavigateTo($"{EditPagePath}{loc}/{id}");
        }

        private async Task Save(ActionEventArgs<BedRequest> args)
        {
            // Placeholder - old grid save removed. Never called.
            await Task.CompletedTask;
        }

        protected async Task Cancel()
        {
            // Placeholder - old grid cancel removed.
            await Task.CompletedTask;
        }

        protected void DataBound()
        {
            if (Grid != null && BedRequests != null)
            {


                if (BedRequests.Count == 0) RecordText = "No BedRequest records found";
                if (Grid.TotalItemCount <= Grid.PageSettings.PageSize)  //compare total grid data count with pagesize value 
                {
                    NoPaging = true;
                }
                else
                {
                    NoPaging = false;
                }
            }
        }

        protected async Task PdfExport()
        {
            if (Grid != null)
            {
                PdfExportProperties exportProperties = new PdfExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("BedRequests", ".pdf"),
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
                    FileName = FileUtil.BuildFileNameWithDate("BedRequests", ".xlsx"),

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
                    FileName = FileUtil.BuildFileNameWithDate("BedRequests", ".csv"),
                };

                await Grid.ExportToCsvAsync(exportProperties);
            }
        }


        private async void DownloadDeliverySheet()
        {
            List<BedRequest> selectedBedRequests = new List<BedRequest>();

            try
            {
                IsGeneratingFile = true;
                StateHasChanged();

                if (!await ValidateScheduled())
                {
                    return;
                }


                selectedBedRequests = await Grid.GetSelectedRecordsAsync();
                int selectedLocation = selectedBedRequests.First().LocationId;
                string? group = selectedBedRequests.First().Group;

                var location = Locations.FirstOrDefault(l => l.LocationId == selectedLocation);
                string? deliveryChecklist = string.Empty;

                var deliveryChecklistResult =
                    await ContentDataService.GetSingleByLocationAndContentType(selectedLocation,
                        ContentType.DeliveryCheckList);

                if (deliveryChecklistResult.Success && deliveryChecklistResult.Data != null)
                {
                    deliveryChecklist = deliveryChecklistResult.Data.ContentHtml;
                }

                var scheduledBedRequestResult =
                    await BedRequestDataService.GetScheduledBedRequestsForLocation(selectedLocation);
                List<BedRequest> scheduledBedRequests =
                    scheduledBedRequestResult.Data.Where(o => o.Group == group).ToList();

                string fileName = DeliverySheetService.CreateDeliverySheetFileName(location, scheduledBedRequests);
                Stream stream =
                    DeliverySheetService.CreateDeliverySheet(location, scheduledBedRequests, deliveryChecklist);
                using var streamRef = new DotNetStreamReference(stream: stream);

                await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading delivery sheet");
                if (ToastService != null)
                {
                    ToastService.Error("Error",
                        "There was an error creating the delivery sheet. Please try again later.");
                }
            }
            finally
            {
                IsGeneratingFile = false;
            }
        }

        private async void DownloadTeamSheet()
        {
            try
            {
                IsGeneratingFile = true;
                StateHasChanged();
                if (!await ValidateScheduled())
                {
                    return;
                }
                var selectedBedRequests = await Grid.GetSelectedRecordsAsync();
                int selectedLocation = selectedBedRequests.First().LocationId;
                string? group = selectedBedRequests.First().Group;
                var location = Locations.FirstOrDefault(l => l.LocationId == selectedLocation);
                string? deliveryChecklist = string.Empty;
                var deliveryChecklistResult = await ContentDataService.GetSingleByLocationAndContentType(selectedLocation, ContentType.DeliveryCheckList);
                if (deliveryChecklistResult.Success && deliveryChecklistResult.Data != null)
                {
                    deliveryChecklist = deliveryChecklistResult.Data.ContentHtml;
                }
                var scheduledBedRequestResult = await BedRequestDataService.GetScheduledBedRequestsForLocation(selectedLocation);
                var scheduledBedRequests = scheduledBedRequestResult.Data.Where(o => o.Group == group).ToList();
                // We will include all teams present in scheduledBedRequests (group already filtered) - if need all groups remove Where above
                string fileName = TeamSheetService.CreateTeamSheetFileName(location, scheduledBedRequests);
                Stream stream = TeamSheetService.CreateTeamSheet(location, scheduledBedRequests, deliveryChecklist);
                using var streamRef = new DotNetStreamReference(stream: stream);
                await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading team sheet");
                ToastService?.Error("Error", "There was an error creating the team sheet. Please try again later.");
            }
            finally
            {
                IsGeneratingFile = false;
            }   
        }

        private async Task<bool> ValidateScheduled()
        {
            if (BedRequest != null && (BedRequests == null || !BedRequests.Any(o => o.Status == BedRequestStatus.Scheduled)))
            {
                DialogHeader = "No Bed Requests";
                DialogContent = "There are no bed requests with a Scheduled status to create the Delivery Sheet.";
                IsDialogVisible = true;
                return false;
            }

            List<BedRequest> selectedBedRequests = new List<BedRequest>();

            if (Grid != null)
            {
                selectedBedRequests = await Grid.GetSelectedRecordsAsync();

                if (!selectedBedRequests.Any())
                {
                    DialogHeader = "Select Row";
                    DialogContent = "Please select a row with the group you would like to schedule.";
                    IsDialogVisible = true;
                    return false;
                }
                else
                {
                    string? selectedGroup = selectedBedRequests.First().Group;
                    if (String.IsNullOrEmpty(selectedGroup))
                    {
                        DialogHeader = "Set Group";
                        DialogContent = "Please edit the selected row and set the Group.";
                        IsDialogVisible = true;
                        return false;
                    }
                }
            }   


            int selectedLocation = selectedBedRequests.First().LocationId;

            var scheduledBedRequestResult = await BedRequestDataService.GetScheduledBedRequestsForLocation(selectedLocation);

            if (!scheduledBedRequestResult.Success || scheduledBedRequestResult.Data == null)
            {
                DialogHeader = "Could Not Load Data";
                DialogContent = scheduledBedRequestResult.Message;
                IsDialogVisible = true;
                return false;
            }

            string? selectedGroupFinal = selectedBedRequests.First().Group;
            // Fix for CS8604: Ensure scheduledBedRequestResult.Data is not null before calling Where
            // Fix for IDE0305: Use collection initializer
            List<BedRequest> scheduledBedRequests = scheduledBedRequestResult.Data != null
                ? scheduledBedRequestResult.Data.Where(o => o.Group == selectedGroupFinal).ToList()
                : new List<BedRequest>();

            return ValidateBedRequestData(scheduledBedRequests, selectedGroupFinal);
        }

        private bool ValidateBedRequestData(List<BedRequest> scheduledBedRequests, string group)
        {
            if (!scheduledBedRequests.Any())
            {
                DialogHeader = "No Bed Requests";
                DialogContent = $"There are no bed requests with a Scheduled status for the selected Group \"{group}\" to create the Delivery Sheet.";
                IsDialogVisible = true;
                return false;
            }

            if (scheduledBedRequests.Any(o => !o.DeliveryDate.HasValue))
            {
                DialogHeader = "Set delivery date";
                DialogContent = $"Please set the delivery date for all Scheduled rows for the selected Group \"{group}\".";
                IsDialogVisible = true;
                return false;
            }

            if (scheduledBedRequests.Any(o => String.IsNullOrEmpty(o.Team)))
            {
                DialogHeader = "Set team number";
                DialogContent = $"Please set the team number for all Scheduled rows for the selected Group \"{group}\"";
                IsDialogVisible = true;
                return false;
            }

            return true;
        }

        private void DialogOkClick()
        {
            IsDialogVisible = false;
        }

        public void OnLanguageChange(ChangeEventArgs<string, string> args)
        {
            SpeakEnglishVisibility = "hidden";
            if (args.Value != null)
            {
                if (args.Value.ToString() != "English")
                {
                    SpeakEnglishVisibility = "visible";
                }
            }
        }
             

        public void OnLocationChange(ChangeEventArgs<int, Location> args)
        {
            if (args.Value > 0 && BedRequest != null && Locations != null)
            {
                var location = Locations.FirstOrDefault(o => o.LocationId == args.Value);

                if (location != null)
                {
                    BedRequest.Group = location.Group;
                    StateHasChanged();
                }
            }
        }
    }
}

