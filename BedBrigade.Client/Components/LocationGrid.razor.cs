using BedBrigade.Common.Constants;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using Action = Syncfusion.Blazor.Grids.Action;
using BedBrigade.Data.Services;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Serilog;

namespace BedBrigade.Client.Components
{
    public partial class LocationGrid : ComponentBase
    {
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private IMetroAreaDataService _svcMetroArea { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ITimezoneDataService _svcTimeZone { get; set; }
        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";

        protected List<Location>? Locations { get; set; }
        protected List<MetroArea>? MetroAreas { get; set; }
        protected List<TimeZoneItem> TimeZones { get; set; }
        protected SfGrid<Location>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? RecordText { get; set; } = "Loading Locations ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "80%", EnableResize = true };

        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
            try
            {
                await LoadData();
            }
            catch (Exception ex)
            {
                Locations= new List<Location>();
                MetroAreas = new List<MetroArea>();
                Log.Error(ex, "Error loading data in LocationGrid component");
                RecordText = "Unable to load locations. " + ex.Message;
                _toastService.Error("Error loading data", ex.Message);
            }
        }

        private async Task LoadData()
        {
            if (_svcAuth.IsNationalAdmin)
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }

            ServiceResponse<List<Location>> result = await _svcLocation.GetAllAsync();
            if (result.Success && result.Data != null)
            {
                Locations = result.Data.ToList();
                var item = Locations.FirstOrDefault(r => r.LocationId == Defaults.NationalLocationId);
                if (item != null)
                {
                    Locations.Remove(item);
                }
            }
            else
            {
                Locations = new List<Location>();
                RecordText = "Unable to load locations. " + result.Message;
                _toastService.Error("Error loading data", result.Message);
            }

            ServiceResponse<List<MetroArea>> metroResult = await _svcMetroArea.GetAllAsync();
            if (metroResult.Success && metroResult.Data != null)
            {
                MetroAreas = metroResult.Data.ToList();
            }
            else
            {
                MetroAreas = new List<MetroArea>();
                RecordText = "Unable to load metro areas. " + metroResult.Message;
                _toastService.Error("Error loading data", metroResult.Message);
            }

            TimeZones = _svcTimeZone.GetTimeZones();
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Location };
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Location, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Location} : {result.Message}");
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

        public async Task OnActionBegin(ActionEventArgs<Location> args)
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
                    Add(args);
                    break;

                case Action.Save:
                    await Save(args);
                    break;

                case Action.BeginEdit:
                    BeginEdit();
                    break;
            }

        }

        private async Task Delete(ActionEventArgs<Location> args)
        {
            List<Location> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                try
                {
                    FileUtil.DeleteMediaSubDirectory(rec.Route, true);
                    var deleteResult = await _svcLocation.DeleteAsync(rec.LocationId);
                    if (deleteResult.Success)
                    {
                        _toastService.Success("Delete Location", $"Location {rec.Name} deleted successfully.");
                    }
                    else
                    {
                        Log.Error($"Unable to delete location {rec.Name}. Reason: {deleteResult.Message}");
                        _toastService.Error("Delete Location", $"Unable to delete location {rec.Name}. Reason: {deleteResult.Message}");
                        args.Cancel = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error deleting location {rec.Name}");
                    _toastService.Error("Delete Location", $"Error deleting location {rec.Name}: {ex.Message}");
                    args.Cancel = true;
                    break;
                }
            }
        }

        private void Add(ActionEventArgs<Location> args)
        {
            HeaderTitle = "Add Location";
            ButtonTitle = "Add Location";
        }

        private async Task Save(ActionEventArgs<Location> args)
        {
            try
            {
                Location Location = args.Data;
                if (Location.LocationId != 0)
                {
                    //Update Location Record
                    await UpdateLocationAsync(Location);
                }
                else
                {
                    await AddNewLocationAsync(Location);
                }

                await Grid.CallStateHasChangedAsync();
                await Grid.Refresh();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving location");
                _toastService.Error("Error", "An error occurred while saving the location: " + ex.Message);
            }
        }

        private async Task AddNewLocationAsync(Location Location)
        {
            Location.Route = Location.Route.StartsWith("/") ? Location.Route : "/" + Location.Route; // VS 8/26/2024

            // new Location
            var result = await _svcLocation.CreateAsync(Location);

            if (result.Success)
            {
                _toastService.Success("Add Location Success", "Location added successfully.");
            }
            else
            {
                Log.Error($"Unable to add location {Location.Name}. Reason: {result.Message}");
                _toastService.Error("Add Location Error", $"Unable to add location {Location.Name}. Reason: {result.Message}");
            }
        }

        private async Task UpdateLocationAsync(Location Location)
        {
            var updateResult = await _svcLocation.UpdateAsync(Location);
            if (updateResult.Success)
            {
                _toastService.Success("Update Location", "Location updated successfully.");
            }
            else
            {
                Log.Error($"Unable to update location {Location.Name}. Reason: {updateResult.Message}");
                _toastService.Error("Update Location", $"Unable to update location {Location.Name}. Reason: {updateResult.Message}");
            }
        }



        private void BeginEdit()
        {
            HeaderTitle = "Update Location";
            ButtonTitle = "Update";
        }

        protected async Task Save(Location location)
        {
            location.Route.ToLower();
            await Grid.EndEditAsync();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEditAsync();
        }

        protected void DataBound()
        {
            if (Locations.Count == 0) RecordText = "No Location records found";
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
                FileName = "Location" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Location " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Location " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }


    }
}

