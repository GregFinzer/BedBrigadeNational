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

namespace BedBrigade.Client.Components
{
    public partial class BedRequestGrid : ComponentBase
    {
        [Inject] private IBedRequestDataService? _svcBedRequest { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private IMetroAreaDataService _svcMetroArea { get; set; }
        [Inject] private IDeliverySheetService _svcDeliverySheet { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }

        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private NavigationManager _nav { get; set; }
        [Inject] private IGeoLocationQueueDataService? _svcGeoLocation { get; set; }
        [Parameter] public string? Id { get; set; }

        private List<UsState>? StateList = AddressHelper.GetStateList();

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";

        protected List<BedRequest>? BedRequests { get; set; }
        protected List<Location>? Locations { get; set; } 
        protected SfGrid<BedRequest>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected List<string>? lstPrimaryLanguage;
        protected List<string>? lstSpeakEnglish;

        protected BedRequest BedRequest { get; set; } = new BedRequest();
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }

        protected bool OnlyRead { get; set; } = false;

        protected string? RecordText { get; set; } = "Loading BedRequests ...";
        public bool NoPaging { get; private set; }
        public string SpeakEnglishVisibility = "hidden";

        public string ManageBedRequestsMessage { get; set; } = "Manage Bed Requests";

        public List<BedRequestEnumItem> BedRequestStatuses { get; private set; }

        protected DialogSettings DialogParams = new DialogSettings { Width = "75%", MinHeight = "725" };
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "3" },
        };

        private bool IsDialogVisible { get; set; } = false;
        private string DialogHeader { get; set; } = string.Empty;
        private string DialogContent { get; set; } = string.Empty;
        public required SfMaskedTextBox zipTextBox;
        public required SfMaskedTextBox phoneTextBox;
        
        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                Log.Information($"{_svcAuth.UserName} went to the Manage Bed Requests Page");

                SetupToolbar();
                await LoadConfiguration();
                await LoadLocations();
                await LoadBedRequests();

                BedRequestStatuses = EnumHelper.GetBedRequestStatusItems();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"BedRequestGrid.OnInitializedAsync");
                _toastService.Error("Error", "An error occurred while initializing the Bed Request Grid.");
            }
        }

        private async Task LoadBedRequests()
        {
            var locationId = _svcUser.GetUserLocationId();

            var userLocationResult = await _svcLocation.GetByIdAsync(locationId);
            if (userLocationResult.Success && userLocationResult.Data != null)
            {
                //If this is a metro user, get all contacts for the metro area
                if (userLocationResult.Data.IsMetroLocation())
                {
                    var metroAreaResult = await _svcMetroArea.GetByIdAsync(userLocationResult.Data.MetroAreaId.Value);

                    if (metroAreaResult.Success && metroAreaResult.Data != null)
                    {
                        if (_svcAuth.UserHasRole(RoleNames.CanManageBedRequests))
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

                    var metroLocations = await _svcLocation.GetLocationsByMetroAreaId(userLocationResult.Data.MetroAreaId.Value);

                    if (metroLocations.Success && metroLocations.Data != null)
                    {
                        var metroAreaLocationIds = metroLocations.Data.Select(l => l.LocationId).ToList();
                        var metroAreaBedRequestResult = await _svcBedRequest.GetAllForLocationList(metroAreaLocationIds);
                        if (metroAreaBedRequestResult.Success && metroAreaBedRequestResult.Data != null)
                        {
                            BedRequests = metroAreaBedRequestResult.Data.ToList();
                        }
                    }

                    return;
                }

                //Get By Location
                var locationResult = await _svcBedRequest.GetAllForLocationAsync(userLocationResult.Data.LocationId);
                if (locationResult.Success)
                {
                    BedRequests = locationResult.Data.ToList();
                    if (_svcAuth.UserHasRole(RoleNames.CanManageBedRequests))
                    {
                        ManageBedRequestsMessage = $"Manage Bed Requests for {userLocationResult.Data.Name}";
                    }
                    else
                    {
                        ManageBedRequestsMessage = $"View Bed Requests for {userLocationResult.Data.Name}";
                    }
                }
            }
        }

        private async Task LoadLocations()
        {
            var locationResult = await _svcLocation.GetActiveLocations();
            if (locationResult.Success)
            {
                Locations = locationResult.Data.ToList();
                var item = Locations.Single(r => r.LocationId == Defaults.NationalLocationId);
                if (item != null)
                {
                    Locations.Remove(item);
                }
            }
        }

        private async Task LoadConfiguration()
        {
            var dataConfiguration = await _svcConfiguration.GetAllAsync(ConfigSection.System); // Configuration ============================
            if (dataConfiguration.Success && dataConfiguration != null)
            {
                var dctConfiguration = dataConfiguration.Data.ToDictionary(keySelector: x => x.ConfigurationKey, elementSelector: x => x.ConfigurationValue);
                string delimitedString = dctConfiguration[ConfigNames.PrimaryLanguage].ToString();
                lstPrimaryLanguage = new List<string>(delimitedString.Split(';'));
                delimitedString = dctConfiguration[ConfigNames.SpeakEnglish].ToString();
                lstSpeakEnglish = new List<string>(delimitedString.Split(';'));
                
            }
        } // Configuration

        private void SetupToolbar()
        {
            if (_svcAuth.UserHasRole(RoleNames.CanManageBedRequests))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset", "Download Delivery Sheet", "Sort Waiting Closest" };
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
                if (_svcAuth.UserHasRole(RoleNames.CanManageBedRequests))
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
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.BedRequest };
            var result = await _svcUserPersist.GetGridPersistence(persist);
            if (result.Success && result.Data != null)
            {
                await Grid.SetPersistDataAsync(result.Data);
            }
            else
            {
                await FilterWaiting(); 
            }
        }

        private async Task FilterWaiting()
        {
            await Grid.FilterByColumnAsync(
                nameof(BedRequest.StatusString), // Column field name
                "equal",                        // Filter operator
                "Waiting"                     // Filter value
            );
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
            _state = await Grid.GetPersistDataAsync();
            string userName = _svcUser.GetUserName();
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
                    await Grid.ResetPersistDataAsync();
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
                case "Download Delivery Sheet":
                    DownloadDeliverySheet();
                    break;
                case "Sort Waiting Closest":
                    await SortClosest();
                    break;
            }
        }

        private async Task SortClosest()
        {
            List<BedRequest> selectedBedRequests = await Grid.GetSelectedRecordsAsync();

            if (!selectedBedRequests.Any())
            {
                DialogHeader = "Select Row";
                DialogContent = "Please select an address row you would like to sort closest.";
                IsDialogVisible = true;
                return;
            }

            BedRequests = _svcBedRequest.SortBedRequestClosestToAddress(BedRequests, selectedBedRequests.First().BedRequestId);
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
                    BedRequest = args.Data;
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
                
                if (deleteResult.Success)
                {
                    _toastService.Success("Delete Successful", "The delete was successful");
                }
                else
                {
                    Log.Error($"Unable to delete BedRequest {rec.BedRequestId} : {deleteResult.Message}");
                    _toastService.Error("Delete Unsuccessful", "The delete was unsucessful");
                    args.Cancel = true;
                }
            }
        }

        private void Add()
        {
            HeaderTitle = @_lc.Keys["Add"]+" "+ @_lc.Keys["BedRequest"];
            ButtonTitle = @_lc.Keys["Add"] + " " + @_lc.Keys["BedRequest"];
            BedRequest.LocationId = _svcAuth.LocationId;
            BedRequest.PrimaryLanguage = "English";

            var location = Locations.FirstOrDefault(o => o.LocationId == _svcAuth.LocationId);

            if (location != null)
            {
                BedRequest.Group = location.Group;
            }
        }


        private async Task Save(ActionEventArgs<BedRequest> args)
        {
            BedRequest bedRequest = args.Data;
            bedRequest.Phone = bedRequest.Phone.FormatPhoneNumber();
            // Set Speak English  to avoid NULL error
            if (bedRequest.PrimaryLanguage == "English")
            {
                bedRequest.SpeakEnglish = "Yes";
            }
            else
            {
                if (String.IsNullOrEmpty(bedRequest.SpeakEnglish))
                {
                    bedRequest.SpeakEnglish = "No";
                }
            }

            if (await CombineDuplicate(bedRequest))
            {
                await Grid.Refresh();
                return;
            }

            if (bedRequest.BedRequestId != 0)
            {
                await UpdateBedRequest(bedRequest);
            }
            else
            {
                await CreateBedRequest(bedRequest);
            }
            await Grid.Refresh();
        }

        private async Task<bool> CombineDuplicate(BedRequest bedRequest)
        {
            if (bedRequest.BedRequestId > 0 || bedRequest.Status != BedRequestStatus.Waiting || String.IsNullOrEmpty(bedRequest.Phone))
            {
                return false;
            }

            BedRequest existingBedRequest = null;

            var existingByPhone = await _svcBedRequest.GetWaitingByPhone(bedRequest.Phone);

            if (existingByPhone.Success && existingByPhone.Data != null)
            {
                existingBedRequest = existingByPhone.Data;
            }
            else if (!String.IsNullOrEmpty(bedRequest.Email))
            {
                var existingByEmail = await _svcBedRequest.GetWaitingByEmail(bedRequest.Email);

                if (existingByEmail.Success && existingByEmail.Data != null)
                {
                    existingBedRequest = existingByEmail.Data;
                }
            }

            if (existingBedRequest == null)
            {
                return false;
            }
            existingBedRequest.UpdateDuplicateFields(bedRequest, $"Updated on {DateTime.Now.ToShortDateString()} by {_svcAuth.UserName}.");

            var updateResult = await _svcBedRequest.UpdateAsync(existingBedRequest);

            if (updateResult.Success && updateResult.Data != null)
            {
                _toastService.Warning("Update Successful", "A duplicate Bed Request with the same phone number or email was updated.");
                ObjectUtil.CopyProperties(updateResult.Data, bedRequest);
                return true;
            }

            Log.Error($"Unable to update BedRequest {BedRequest.BedRequestId} : {updateResult.Message}");
            _toastService.Error("Update Unsuccessful", "The BedRequest was not updated successfully");

            return false;
        }

        private async Task CreateBedRequest(BedRequest BedRequest)
        {
            var result = await _svcBedRequest.CreateAsync(BedRequest);
            if (result.Success)
            {
                BedRequest = result.Data;
            }
            if (BedRequest.BedRequestId != 0)
            {
                bool success = await QueueForGeoLocation(BedRequest);
                if (!success)
                {
                    return;
                }
                _toastService.Success("BedRequest Created", "BedRequest Created Successfully!");
            }
            else
            {
                Log.Error($"Unable to create BedRequest : {result.Message}");
                _toastService.Error("BedRequest Not Created", "BedRequest was not created successfully");
            }
        }

        private async Task<bool> QueueForGeoLocation(Common.Models.BedRequest bedRequest)
        {
            GeoLocationQueue item = new GeoLocationQueue();
            item.Street = bedRequest.Street;
            item.City = bedRequest.City;
            item.State = bedRequest.State;
            item.PostalCode = bedRequest.PostalCode;
            item.CountryCode = Defaults.CountryCode;
            item.TableName = TableNames.BedRequests.ToString();
            item.TableId = bedRequest.BedRequestId;
            item.QueueDate = DateTime.UtcNow;
            item.Priority = 1;
            item.Status = GeoLocationStatus.Queued.ToString();
            var result = await _svcGeoLocation.CreateAsync(item);

            if (!result.Success)
            {
                string message = "Created bed request but could not queue for Geolocation: " + result.Message;
                Log.Error(message);
                _toastService.Error("GeoLocation Failure", message);
                return false;
            }

            return true;
        }


        private async Task UpdateBedRequest(BedRequest BedRequest)
        {
            var updateResult = await _svcBedRequest.UpdateAsync(BedRequest);

            if (updateResult.Success)
            {
                bool success = await QueueForGeoLocation(BedRequest);
                if (!success)
                {
                    return;
                }

                _toastService.Success("Update Successful", "The BedRequest was updated successfully");
            }
            else
            {
                Log.Error($"Unable to update BedRequest {BedRequest.BedRequestId} : {updateResult.Message}");
                _toastService.Error("Update Unsuccessful", "The BedRequest was not updated successfully");
            }
        }

        private void BeginEdit()
        {
            HeaderTitle = @_lc.Keys["Update"] + " " + @_lc.Keys["BedRequest"];
            ButtonTitle = @_lc.Keys["Update"];
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
            try
            {

                if (!await ValidateScheduled())
                {
                    return;
                }

                List<BedRequest> selectedBedRequests = await Grid.GetSelectedRecordsAsync();
                int selectedLocation = selectedBedRequests.First().LocationId;
                string group = selectedBedRequests.First().Group;

                var location = Locations.FirstOrDefault(l => l.LocationId == selectedLocation);
                string deliveryChecklist = string.Empty;

                var deliveryChecklistResult =
                    await _svcContent.GetSingleByLocationAndContentType(selectedLocation,
                        ContentType.DeliveryCheckList);

                if (deliveryChecklistResult.Success && deliveryChecklistResult.Data != null)
                {
                    deliveryChecklist = deliveryChecklistResult.Data.ContentHtml;
                }

                var scheduledBedRequestResult =
                    await _svcBedRequest.GetScheduledBedRequestsForLocation(selectedLocation);
                List<BedRequest> scheduledBedRequests = scheduledBedRequestResult.Data.Where(o => o.Group == group).ToList();

                string fileName =
                    _svcDeliverySheet.CreateDeliverySheetFileName(location, scheduledBedRequests);
                Stream stream =
                    _svcDeliverySheet.CreateDeliverySheet(location, scheduledBedRequests, deliveryChecklist);
                using var streamRef = new DotNetStreamReference(stream: stream);

                await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading delivery sheet");
                _toastService.Error("Error", "There was an error creating the delivery sheet. Please try again later.");
            }
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

            List<BedRequest> selectedBedRequests = await Grid.GetSelectedRecordsAsync();

            if (!selectedBedRequests.Any())
            {
                DialogHeader = "Select Row";
                DialogContent = "Please select a row with the group you would like to schedule.";
                IsDialogVisible = true;
                return false;
            }

            string group = selectedBedRequests.First().Group;
            if (String.IsNullOrEmpty(group))
            {
                DialogHeader = "Set Group";
                DialogContent = "Please edit the selected row and set the Group.";
                IsDialogVisible = true;
                return false;
            }


            int selectedLocation = selectedBedRequests.First().LocationId;

            var scheduledBedRequestResult = await _svcBedRequest.GetScheduledBedRequestsForLocation(selectedLocation);

            if (!scheduledBedRequestResult.Success || scheduledBedRequestResult.Data == null)
            {
                DialogHeader = "Could Not Load Data";
                DialogContent = scheduledBedRequestResult.Message;
                IsDialogVisible = true;
                return false;
            }

            List<BedRequest> scheduledBedRequests = scheduledBedRequestResult.Data.Where(o => o.Group == group).ToList();
            return ValidateBedRequestData(scheduledBedRequests, group);
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
        public async Task HandlePhoneMaskFocus()
        {
            await JS.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
        }

        public async Task HandleZipMaskFocus()
        {
            await JS.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", zipTextBox.ID, 0);
        }

        public void OnLocationChange(ChangeEventArgs<int, Location> args)
        {
            if (args.Value != null && args.Value > 0 && BedRequest != null)
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

