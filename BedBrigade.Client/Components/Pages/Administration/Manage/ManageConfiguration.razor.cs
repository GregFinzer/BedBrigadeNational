using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using BedBrigade.Data.Services;
using Serilog;
using Action = Syncfusion.Blazor.Grids.Action;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components.Pages.Administration.Manage
{
    public partial class ManageConfiguration : ComponentBase
    {
        private const string AddEditConfigurationPageBaseUrl = "/administration/admintasks/addeditconfiguration/";

        [Inject] private IConfigurationDataService _svcConfiguration { get; set; } = default!;
        [Inject] private IUserDataService _svcUser { get; set; } = default!;
        [Inject] private IUserPersistDataService _svcUserPersist { get; set; } = default!;
        [Inject] private IAuthService _svcAuth { get; set; } = default!;
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;
        [Inject] private ILocationDataService _svcLocation { get; set; } = default!;
        [Inject] private ToastService _toastService { get; set; } = default!;

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";

        protected IEnumerable<Configuration>? ConfigRecs { get; set; }
        protected SfGrid<Configuration>? Grid { get; set; }
        protected List<string> ToolBar { get; set; } = new();
        protected List<string> ContextMenu { get; set; } = new();
        protected string? GridState { get; set; }

        protected bool NoPaging { get; private set; }
        protected string? RecordText { get; set; } = "Loading Configuration ...";

        protected List<Location> Locations { get; set; } = new List<Location>();
        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Log.Information($"{_svcAuth.UserName} went to the Manage Configurations Page");

                if (_svcAuth.IsNationalAdmin)
                {
                    ToolBar = new List<string>
                    {
                        "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset"
                    };
                    ContextMenu = new List<string>
                    {
                        "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll",
                        "SortAscending", "SortDescending"
                    }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
                }
                else
                {
                    ToolBar = new List<string> { "Search", "Reset" };
                    ContextMenu = new List<string>
                    {
                        FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending",
                        "SortDescending"
                    }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
                }

                await LoadData();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ConfigurationGrid component");
                _toastService.Error("Error initializing Configuration Grid", ex.Message);
            }
        }

        private async Task LoadData()
        {
            //This GetAllAsync should always have less than 1000 records
            var result = await _svcConfiguration.GetAllAsync();
            if (result.Success && result.Data != null)
            {
                ConfigRecs = result.Data.ToList();
            }
            else
            {
                Log.Error($"Unable to load configurations: {result.Message}");
                _toastService.Error("Unable to load configurations", result.Message);
            }

            var locationResult = await _svcLocation.GetActiveLocations();
            if (locationResult.Success && locationResult.Data != null)
            {
                Locations = locationResult.Data.ToList();
            }
            else
            {
                Log.Error($"Unable to load locations: {locationResult.Message}");
                _toastService.Error("Unable to load locations", locationResult.Message);
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || Grid == null)
            {
                return base.OnAfterRenderAsync(firstRender);
            }

            if (_svcAuth.IsNationalAdmin)
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Configuration };
            var result = await _svcUserPersist.GetGridPersistence(persist);
            if (result.Success && result.Data != null && Grid != null)
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
            if (Grid == null)
            {
                return;
            }

            GridState = await Grid.GetPersistDataAsync();
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Configuration, Data = GridState };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Configuration} : {result.Message}");
            }
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (Grid == null)
            {
                return;
            }

            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistDataAsync();
                await SaveGridPersistence();
                return;
            }

            if (args.Item.Text == "Pdf Export")
            {
                await PdfExport();
                return;
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

        public async Task OnActionBegin(ActionEventArgs<Configuration> args)
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
                    await EditAsync(args);
                    break;
            }

        }

        private async Task Delete(ActionEventArgs<Configuration> args)
        {
            try
            {
                if (Grid == null)
                {
                    args.Cancel = true;
                    return;
                }

                List<Configuration> records = await Grid.GetSelectedRecordsAsync();
                foreach (var rec in records)
                {
                    var deleteResult = await _svcConfiguration.DeleteAsync(rec.ConfigurationId);
                    if (deleteResult.Success)
                    {
                        _toastService.Success("Delete Configuration", "Configuration Deleted Successfully!");
                        await RefreshGridAsync();
                    }
                    else
                    {
                        _toastService.Error("Delete Configuration", "Unable to delete configuration!");
                        args.Cancel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting configuration");
                _toastService.Error("Delete Configuration", "An error occurred while deleting the configuration.");
                args.Cancel = true;
            }
        }

        private void Add()
        {
            _navigationManager.NavigateTo($"{AddEditConfigurationPageBaseUrl}{Defaults.NationalLocationId}");
        }

        private async Task EditAsync(ActionEventArgs<Configuration> args)
        {
            args.Cancel = true;

            Configuration? configuration = args.Data;
            if (configuration == null && Grid != null)
            {
                List<Configuration> records = await Grid.GetSelectedRecordsAsync();
                configuration = records.FirstOrDefault();
            }

            if (configuration == null)
            {
                _toastService.Error("Edit Configuration", "Select a configuration record to edit.");
                return;
            }

            _navigationManager.NavigateTo($"{AddEditConfigurationPageBaseUrl}{configuration.LocationId}/{configuration.ConfigurationId}");
        }

        private async Task RefreshGridAsync()
        {
            await LoadData();

            if (Grid != null)
            {
                await Grid.Refresh();
            }

            StateHasChanged();
        }

        protected void DataBound()
        {
            if (ConfigRecs?.Any() != true)
            {
                RecordText = "No configurations found";
            }

            if (Grid == null)
            {
                return;
            }

            NoPaging = Grid.TotalItemCount <= Grid.PageSettings.PageSize;

            // await Grid.AutoFitColumns();
        }

        protected async Task PdfExport()
        {
            if (Grid != null)
            {
                PdfExportProperties exportProperties = new PdfExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("Configuration", ".pdf"),
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
                    FileName = FileUtil.BuildFileNameWithDate("Configuration", ".xlsx"),
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
                    FileName = FileUtil.BuildFileNameWithDate("Configuration", ".csv"),
                };

                await Grid.ExportToCsvAsync(exportProperties);
            }
        }

    }
}
