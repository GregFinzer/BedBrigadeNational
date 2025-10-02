using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.Grids;
using Action = Syncfusion.Blazor.Grids.Action;
using Task = System.Threading.Tasks.Task;

namespace BedBrigade.Client.Components.Pages.Administration.Manage
{
    public partial class ManageDonationCampaigns : ComponentBase
    {
        [Inject] private IDonationCampaignDataService _svcDonationCampaign { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ToastService? _toastService { get; set; }
        protected List<DonationCampaign>? DonationCampaignRecords { get; set; }
        protected SfGrid<DonationCampaign>? Grid { get; set; }

        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        protected List<Location> Locations { get; set; } = new List<Location>();
        protected string? _state { get; set; }
        protected string? RecordText { get; set; } = "Loading DonationCampaigns ...";
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected bool AddMode { get; set; } = false;
        protected bool NoPaging { get; private set; }
        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };
        public bool IsLocationColumnVisible { get; set; } = false;
        protected override async Task OnInitializedAsync()
        {
            Log.Information($"{_svcAuth.UserName} went to the Manage DonationCampaigns Page");

            var locationResult = await _svcLocation.GetActiveLocations();
            if (locationResult.Success && locationResult.Data != null)
            {
                Locations = locationResult.Data;
            }
            else
            {
                Log.Error("ManageDonationCampaigns, Could not load locations: " + locationResult.Message);
                _toastService?.Error("Error Loading Locations", "Could not load locations: " + locationResult.Message);
            }

            ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
            ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };



            //Get all records when an admin
            if (_svcAuth.IsNationalAdmin)
            {
                IsLocationColumnVisible = true;
                //This GetAllAsync should always have less than 1000 records
                var result = await _svcDonationCampaign.GetAllAsync();
                if (result.Success)
                {
                    DonationCampaignRecords = result.Data;
                }
                else
                {
                    Log.Error("ManageDonationCampaigns, Could not load Donation Campaigns: " + result.Message);
                    _toastService?.Error("Error Loading DonationCampaigns", "Could not load Donation Campaigns: " + result.Message);
                }
            }
            else
            {
                var locationId = _svcUser.GetUserLocationId();
                var result = await _svcDonationCampaign.GetAllForLocationAsync(locationId);
                if (result.Success)
                {
                    DonationCampaignRecords = result.Data;
                }
                else
                {
                    Log.Error("ManageDonationCampaigns, Could not load Donation Campaigns for location: " + result.Message);
                    _toastService?.Error("Error Loading DonationCampaigns", "Could not load Donation Campaigns for location: " + result.Message);
                }
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                Grid.EditSettings.AllowEditOnDblClick = true;
                Grid.EditSettings.AllowDeleting = true;
                Grid.EditSettings.AllowAdding = true;
                Grid.EditSettings.AllowEditing = true;
                StateHasChanged();
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.DonationCampaign };
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.DonationCampaign, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.DonationCampaign} : {result.Message}");
            }
        }

        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistData();
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

        public async Task OnActionBegin(ActionEventArgs<DonationCampaign> args)
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

        private void BeginEdit()
        {
            HeaderTitle = "Update Donation Campaign";
            ButtonTitle = "Update Donation Campaign";
            AddMode = false;
        }

        private async Task Delete(ActionEventArgs<DonationCampaign> args)
        {
            List<DonationCampaign> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcDonationCampaign.DeleteAsync(rec.DonationCampaignId);

                if (deleteResult.Success)
                {
                    _toastService.Success("DonationCampaign Deleted", $"Donation Campaign Deleted: " + rec.CampaignName);
                }
                else
                {
                    Log.Error($"Error deleting Donation Campaigns {rec.CampaignName}: {deleteResult.Message}");
                    _toastService.Success("Error Deleting", $"Donation Campaign could not be deleted: " + rec.CampaignName);
                    args.Cancel = true;
                }
            }
        }

        private void Add()
        {
            HeaderTitle = "Add DonationCampaign";
            ButtonTitle = "Add DonationCampaign";
            AddMode = true;
        }

        private async Task Save(ActionEventArgs<DonationCampaign> args)
        {
            DonationCampaign donationCampaign = args.Data;

            // Basic required validations (client side also in Razor)
            if (string.IsNullOrWhiteSpace(donationCampaign.CampaignName))
            {
                _toastService?.Warning("Missing Name", "Campaign Name is required.");
                args.Cancel = true;
                return;
            }

            if (donationCampaign.EndDate.HasValue && donationCampaign.EndDate.Value < donationCampaign.StartDate)
            {
                _toastService?.Warning("Invalid Dates", "End Date cannot be earlier than Start Date.");
                args.Cancel = true;
                return;
            }

            if (!AddMode)
            {
                //Update DonationCampaign Record
                var updateResult = await _svcDonationCampaign.UpdateAsync(donationCampaign);
                if (updateResult.Success)
                {
                    _toastService.Success("Donation Campaign Updated", $"Donation Campaign Updated: " + donationCampaign.CampaignName);
                }
                else
                {
                    Log.Error($"Error updating DonationCampaign {donationCampaign.CampaignName}: {updateResult.Message}");
                    _toastService.Error("Error Updating", $"Could not update Donation Campaign: " + donationCampaign.CampaignName);
                }
            }
            else
            {
                // New DonationCampaign
                var createResult = await _svcDonationCampaign.CreateAsync(donationCampaign);

                if (createResult.Success)
                {
                    _toastService.Success("Donation Campaign Updated", $"DonationCampaign Added: " + donationCampaign.CampaignName);
                }
                else
                {
                    Log.Error($"Error adding DonationCampaign {donationCampaign.CampaignName}: {createResult.Message}");
                    _toastService.Error("Error Adding", $"Could not add Donation Campaign: " + donationCampaign.CampaignName);
                }
            }
        }
        protected async Task Save(DonationCampaign need)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected void DataBound()
        {
            if (DonationCampaignRecords.ToList().Count == 0) RecordText = "No Donation Campaigns found";
            if (Grid.TotalItemCount <= Grid.PageSettings.PageSize)
            {
                NoPaging = true;
            }
            else
            {
                NoPaging = false;
            }

            Grid.AutoFitColumnsAsync();
        }

        protected async Task PdfExport()
        {
            if (Grid != null)
            {
                PdfExportProperties exportProperties = new PdfExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("DonationCampaigns", ".pdf"),
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
                    FileName = FileUtil.BuildFileNameWithDate("DonationCampaigns", ".xlsx"),
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
                    FileName = FileUtil.BuildFileNameWithDate("DonationCampaigns", ".csv"),
                };

                await Grid.ExportToCsvAsync(exportProperties);
            }
        }

    }
}
