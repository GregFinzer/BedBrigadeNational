using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using BedBrigade.Data.Services;
using Serilog;
using Action = Syncfusion.Blazor.Grids.Action;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components
{
    public partial class ConfigurationGrid : ComponentBase
    {
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ToastService _toastService { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";

        protected IEnumerable<Configuration>? ConfigRecs { get; set; }
        protected SfGrid<Configuration>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }

        protected bool NoPaging { get; private set; }
        protected bool AddKey { get; set; } = false;
        protected string? RecordText { get; set; } = "Loading Configuration ...";
        protected string? Hide { get; private set; } = "true";
        public List<ConfigSectionEnumItem> ConfigSectionEnumItems { get; private set; }

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };

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

                _lc.InitLocalizedComponent(this);
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

                ConfigSectionEnumItems = EnumHelper.GetConfigSectionItems();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ConfigurationGrid component");
                _toastService.Error("Error initializing Configuration Grid", ex.Message);
            }
        }

        private async Task LoadData()
        {
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
            if (!firstRender)
            {
                if (_svcAuth.IsNationalAdmin)
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Configuration };
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Configuration, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Configuration} : {result.Message}");
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

        private async Task Delete(ActionEventArgs<Configuration> args)
        {
            try
            {
                List<Configuration> records = await Grid.GetSelectedRecordsAsync();
                foreach (var rec in records)
                {
                    var deleteResult = await _svcConfiguration.DeleteAsync(rec.ConfigurationId);
                    if (deleteResult.Success)
                    {
                        _toastService.Success("Delete Configuration", "Configuration Deleted Successfully!");
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
            HeaderTitle = "Add Configuration";
            ButtonTitle = "Add Configuration";
            AddKey = true;
        }

        private async Task Save(ActionEventArgs<Configuration> args)
        {
            try
            {

                Configuration Configuration = args.Data;
                if (!string.IsNullOrEmpty(Configuration.ConfigurationKey) && !AddKey)
                {
                    //Update Configuration Record
                    var updateResult = await _svcConfiguration.UpdateAsync(Configuration);
                    if (updateResult.Success)
                    {
                        _toastService.Success("Update Configuration Success", "Configuration Updated Successfully!");
                    }
                    else
                    {
                        _toastService.Error("Update Configuration Error", "Unable to update configuration!");
                    }
                }
                else
                {
                    var existing = await _svcConfiguration.GetAllForLocationAsync(Configuration.LocationId);

                    if (existing.Success 
                        && existing.Data != null 
                        && existing.Data.Any(c => c.ConfigurationKey == Configuration.ConfigurationKey))
                    {
                        _toastService.Error("Add Configuration Error", "Configuration Key already exists for this location!");
                        args.Cancel = true;
                        return;
                    }

                    // new Configuration
                    var createResult = await _svcConfiguration.CreateAsync(Configuration);
                    if (createResult.Success)
                    {
                        _toastService.Success("Add Configuration Success", "Configuration Added Successfully!");
                    }
                    else
                    {
                        _toastService.Error("Add Configuration Error", "Unable to add configuration!");
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving configuration");
                _toastService.Error("Save Configuration", "An error occurred while saving the configuration.");
            }
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update Configuration";
            ButtonTitle = "Update Configuration";
            AddKey = false;
        }

        protected async Task Save(Configuration need)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected void DataBound()
        {
            if (ConfigRecs.ToList().Count == 0) RecordText = "No configurations found";
            if (Grid.TotalItemCount <= Grid.PageSettings.PageSize)
            {
                NoPaging = true;
            }
            else
            {
                NoPaging = false;
            }

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

                await Grid.CsvExport(exportProperties);
            }
        }


    }
}

