using BedBrigade.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using BedBrigade.Data.Services;
using Serilog;
using Action = Syncfusion.Blazor.Grids.Action;

using BedBrigade.Common.Logic;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Models;
using Microsoft.JSInterop;
using System.IO;

namespace BedBrigade.Client.Components
{
    public partial class BedRequestGrid : ComponentBase
    {
        [Inject] private IBedRequestDataService? _svcBedRequest { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private IMetroAreaDataService _svcMetroArea { get; set; }
        [Inject] private IHeaderMessageService _headerMessageService { get; set; }
        [Inject] private IDeliverySheetService _svcDeliverySheet { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }

        [Inject] private IJSRuntime JS { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
        protected List<BedRequest>? BedRequests { get; set; }
        protected List<Location>? Locations { get; set; }
        protected SfGrid<BedRequest>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected BedRequest BedRequest { get; set; } = new BedRequest();
        protected string[] groupColumns = new string[] { "LocationId" };
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? addNeedDisplay { get; private set; }
        protected string? editNeedDisplay { get; private set; }
        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; } = 3000;
        protected bool OnlyRead { get; set; } = false;

        protected string? RecordText { get; set; } = "Loading BedRequests ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }
        public bool IsLocationColumnVisible { get; set; } = false;

        public string ManageBedRequestsMessage { get; set; } = "Manage Bed Requests";

        public List<BedRequestEnumItem> BedRequestStatuses { get; private set; }

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };

        private bool IsDialogVisible { get; set; } = false;
        private string DialogHeader { get; set; } = string.Empty;
        private string DialogContent { get; set; } = string.Empty;

        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState.GetAuthenticationStateAsync();
            Identity = authState.User;
            var userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Manage Bed Requests Page");

            SetupToolbar();
            await LoadLocations();
            await LoadBedRequests();

            BedRequestStatuses = EnumHelper.GetBedRequestStatusItems();
        }

        private async Task LoadBedRequests()
        {
            bool isNationalAdmin = await _svcUser.IsUserNationalAdmin();

            //Get all records when an admin
            if (isNationalAdmin)
            {
                var allResult = await _svcBedRequest.GetAllAsync();

                if (allResult.Success)
                {
                    BedRequests = allResult.Data.ToList();
                    IsLocationColumnVisible = true;
                    ManageBedRequestsMessage = "Manage Bed Requests Nationally";
                }
                return;
            }

            var locationId = await _svcUser.GetUserLocationId();

            var userLocationResult = await _svcLocation.GetByIdAsync(locationId);
            if (userLocationResult.Success && userLocationResult.Data != null)
            {
                //If this is a metro user, get all contacts for the metro area
                if (userLocationResult.Data.IsMetroLocation())
                {
                    var metroAreaResult = await _svcMetroArea.GetByIdAsync(userLocationResult.Data.MetroAreaId.Value);

                    if (metroAreaResult.Success && metroAreaResult.Data != null)
                    {
                        ManageBedRequestsMessage = $"Manage Bed Requests for the {metroAreaResult.Data.Name} Metro Area";
                    }

                    var metroLocations = await _svcLocation.GetLocationsByMetroAreaId(userLocationResult.Data.MetroAreaId.Value);

                    if (metroLocations.Success && metroLocations.Data != null)
                    {
                        var metroAreaLocationIds = metroLocations.Data.Select(l => l.LocationId).ToList();
                        var metroAreaBedRequestResult = await _svcBedRequest.GetAllForLocationList(metroAreaLocationIds);
                        if (metroAreaBedRequestResult.Success && metroAreaBedRequestResult.Data != null)
                        {
                            BedRequests = metroAreaBedRequestResult.Data.ToList();
                            IsLocationColumnVisible = true;
                        }
                    }

                    return;
                }

                //Get By Location
                var locationResult = await _svcBedRequest.GetAllForLocationAsync(userLocationResult.Data.LocationId);
                if (locationResult.Success)
                {
                    BedRequests = locationResult.Data.ToList();
                    ManageBedRequestsMessage = $"Manage Bed Requests for {userLocationResult.Data.Name}";
                }
            }
        }

        private async Task LoadLocations()
        {
            var locationResult = await _svcLocation.GetAllAsync();
            if (locationResult.Success)
            {
                Locations = locationResult.Data.ToList();
                var item = Locations.Single(r => r.LocationId == (int)LocationNumber.National);
                if (item != null)
                {
                    Locations.Remove(item);
                }
            }
        }

        private void SetupToolbar()
        {
            if (Identity.HasRole(RoleNames.CanManageBedRequests))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset", "Download Delivery Sheet" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.IsInRole(RoleNames.NationalAdmin) || Identity.IsInRole(RoleNames.LocationAdmin) || Identity.IsInRole(RoleNames.LocationScheduler))
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
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            string userName = await _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.BedRequest };
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.BedRequest, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.BedRequest} : {result.Message}");
            }
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            switch (args.Item.Text)
            {
                case "Reset":
                    await Grid.ResetPersistData();
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
                case "Download Delivery Sheet":
                    DownloadDeliverySheet();
                    break;
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

        private async Task Delete(ActionEventArgs<BedRequest> args)
        {
            List<BedRequest> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcBedRequest.DeleteAsync(rec.BedRequestId);
                ToastTitle = "Delete BedRequest";
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    ToastContent = $"Unable to Delete. BedRequest is in use.";
                    args.Cancel = true;
                }
                ToastTimeout = 6000;
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

            }
        }

        private void Add()
        {
            HeaderTitle = "Add Bed Request";
            ButtonTitle = "Add Bed Request";
            BedRequest.LocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
        }


        private async Task Save(ActionEventArgs<BedRequest> args)
        {
            BedRequest BedRequest = args.Data;
            BedRequest.Phone = BedRequest.Phone.FormatPhoneNumber();
            if (BedRequest.BedRequestId != 0)
            {
                //Update BedRequest Record
                var updateResult = await _svcBedRequest.UpdateAsync(BedRequest);
                ToastTitle = "Update BedRequest";
                if (updateResult.Success)
                {
                    ToastContent = "BedRequest Updated Successfully!";
                }
                else
                {
                    ToastContent = "Unable to update BedRequest!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            else
            {
                // new BedRequest
                var result = await _svcBedRequest.CreateAsync(BedRequest);
                if (result.Success)
                {
                    BedRequest = result.Data;
                }
                ToastTitle = "Create BedRequest";
                if (BedRequest.BedRequestId != 0)
                {
                    ToastContent = "BedRequest Created Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save BedRequest!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            await Grid.Refresh();
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update Bed Request";
            ButtonTitle = "Update";
        }

        protected async Task Save(BedRequest BedRequest)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected void DataBound()
        {
            if (BedRequests.Count == 0) RecordText = "No BedRequest records found";
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
                FileName = "BedRequest" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "BedRequest " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "BedRequest " + DateTime.Now.ToShortDateString() + ".csv",

            };
            if (Grid != null)
            {
                await Grid.CsvExport(ExportProperties);
            }
        }


        private async void DownloadDeliverySheet()
        {
            if (!await ValidateScheduled())
            {
                return;
            }

            int selectedLocation;
            if (IsLocationColumnVisible)
            {
                List<BedRequest> selectedBedRequests = await Grid.GetSelectedRecordsAsync();
                selectedLocation = selectedBedRequests.First().LocationId;
            }
            else
            {
                selectedLocation = await _svcUser.GetUserLocationId();
            }

            var location = Locations.FirstOrDefault(l => l.LocationId == selectedLocation);
            string deliveryChecklist = string.Empty;

            var deliveryChecklistResult = await _svcContent.GetDeliveryChecklistByLocationId(selectedLocation);

            if (deliveryChecklistResult.Success && deliveryChecklistResult.Data != null)
            {
                deliveryChecklist = deliveryChecklistResult.Data.ContentHtml;
            }

            var scheduledBedRequestResult = await _svcBedRequest.GetScheduledBedRequestsForLocation(selectedLocation);
            string fileName = _svcDeliverySheet.CreateDeliverySheetFileName(location, scheduledBedRequestResult.Data);
            Stream stream = _svcDeliverySheet.CreateDeliverySheet(location, scheduledBedRequestResult.Data, deliveryChecklist);
            using var streamRef = new DotNetStreamReference(stream: stream);

            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);

        }

        private async Task<bool> ValidateScheduled()
        {
            if (!BedRequests.Any(o => o.Status == BedRequestStatus.Scheduled))
            {
                DialogHeader = "No Bed Requests";
                DialogContent = "There are no bed requests with a Scheduled status to create the Delivery Sheet.";
                IsDialogVisible = true;
                return false;
            }

            int selectedLocation;
            if (IsLocationColumnVisible)
            {
                List<BedRequest> selectedBedRequests = await Grid.GetSelectedRecordsAsync();

                if (!selectedBedRequests.Any())
                {
                    DialogHeader = "Select Row";
                    DialogContent = "Please select a row in the location you would like to schedule.";
                    IsDialogVisible = true;
                    return false;
                }
                selectedLocation = selectedBedRequests.First().LocationId;
            }
            else
            {
                selectedLocation = await _svcUser.GetUserLocationId();
            }

            var scheduledBedRequestResult = await _svcBedRequest.GetScheduledBedRequestsForLocation(selectedLocation);

            if (!scheduledBedRequestResult.Success || scheduledBedRequestResult.Data == null)
            {
                DialogHeader = "Could Not Load Data";
                DialogContent = scheduledBedRequestResult.Message;
                IsDialogVisible = true;
                return false;
            }

            return ValidateBedRequestData(scheduledBedRequestResult);
        }

        private bool ValidateBedRequestData(ServiceResponse<List<BedRequest>> scheduledBedRequestResult)
        {
            if (!scheduledBedRequestResult.Data.Any())
            {
                DialogHeader = "No Bed Requests";
                DialogContent = "There are no bed requests with a Scheduled status for the selected location to create the Delivery Sheet.";
                IsDialogVisible = true;
                return false;
            }

            if (scheduledBedRequestResult.Data.Any(o => !o.DeliveryDate.HasValue))
            {
                DialogHeader = "Set delivery date";
                DialogContent = "Please set the delivery date for all scheduled rows.";
                IsDialogVisible = true;
                return false;
            }

            if (scheduledBedRequestResult.Data.Any(o => !o.TeamNumber.HasValue))
            {
                DialogHeader = "Set team number";
                DialogContent = "Please set the team number for all scheduled rows.";
                IsDialogVisible = true;
                return false;
            }

            return true;
        }

        private void DialogOkClick()
        {
            IsDialogVisible = false;
        }
    }
}

