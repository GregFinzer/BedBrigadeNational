using BedBrigade.Data.Models;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using BedBrigade.Data.Services;
using Action = Syncfusion.Blazor.Grids.Action;
using static BedBrigade.Common.Common;
using Serilog;


namespace BedBrigade.Client.Components
{
    public partial class PagesGrid : ComponentBase
    {
        [Inject] private IContentDataService? _svcContent { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IWebHostEnvironment _svcEnv { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private NavigationManager? _navigationManager { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
        protected List<Content>? Pages { get; set; }
        protected Content content { get; set; }
        protected Content CurrentValues { get; set; }
        protected SfGrid<Content>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected string[] groupColumns = new string[] { "LocationId" };
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        public string EditTitle { get; private set; }
        protected string? addNeedDisplay { get; private set; }
        protected string? editNeedDisplay { get; private set; }
        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; } = 3000;
        protected string? RecordText { get; set; } = "Loading Pages ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }
        public List<Location> Locations { get; private set; }
        private string? saveUrl { get; set; }
        public string imagePath { get; private set; }
        public List<ContentTypeEnumItem> ContentTypes { get; private set; }

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };
        private bool AddContentVisible;

        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState.GetAuthenticationStateAsync();
            Identity = authState.User;
            if (Identity.HasRole(RoleNames.CanManagePages))
            {
                ToolBar = new List<string> { "Add", "Edit", "Rename", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Rename", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            content = new Content();
            var result = await _svcContent.GetAllAsync();
            if (result.Success)
            {
                Pages = result.Data.ToList();
            }
            var locResult = await _svcLocation.GetAllAsync();
            if (locResult.Success)
            {
                Locations = locResult.Data;
            }

            ContentTypes = GetContentTypeItems();
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.HasRole(RoleNames.CanManagePages))
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
        /// On loading of the Grid get the user grid persited data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            var result = await _svcUser.GetGridPersistance(new Persist { GridId = (int)PersistGrid.Pages, UserState = await Grid.GetPersistDataAsync() });
            if (result.Success)
            {
                await Grid.SetPersistData(result.Data);
            }
        }

        /// <summary>
        /// On destoring of the grid save its current state
        /// </summary>
        /// <returns></returns>
        protected async Task OnDestroyed()
        {
            _state = await Grid.GetPersistData();
            var result = await _svcUser.SaveGridPersistance(new Persist { GridId = (int)PersistGrid.Pages, UserState = _state });
            if (!result.Success)
            {
                //Log the results
            }

        }




        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {

            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistData();
                _state = await Grid.GetPersistData();
                await _svcUser.SaveGridPersistance(new Persist { GridId = (int)Common.Common.PersistGrid.Pages, UserState = _state });
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

            if (args.Item.Text == "Rename")
            { 
                await RenamePage();
                return;
            }
        }

        public async Task OnActionBegin(ActionEventArgs<Content> args)
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
                    await Add(args);
                    args.Cancel = true;
                    break;

                case Action.BeginEdit:
                    await BeginEdit(args);
                    break;
            }

        }

        private async Task Delete(ActionEventArgs<Content> args)
        {
            string reason = string.Empty;
            List<Content> records = await Grid.GetSelectedRecordsAsync();
            ToastTitle = "Delete Page";
            ToastTimeout = 6000;
            ToastContent = $"Unable to Delete. {reason}";
            foreach (var rec in records)
            {
                try
                {
                    var deleteResult = await _svcContent.DeleteAsync(rec.ContentId);
                    if (deleteResult.Success)
                    {
                        ToastContent = "Delete Successful!";
                        var locationRoute = Locations.Find(l => l.LocationId == rec.LocationId).Route;
                        var folderPath = $"{_svcEnv.ContentRootPath}/wwwroot/media{locationRoute}/pages/{rec.Name}";
                        DeleteDirectory(folderPath);
                        Log.Information($"Deleted Page Folder at {folderPath}");
                    }
                    else
                    {
                        args.Cancel = true;
                        return;
                    }

                }
                catch (Exception ex) 
                {
                    args.Cancel = true;
                    reason = ex.Message;
                    Log.Information($"Error: {reason}");
                    return;
                }

                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

            }
        }

        private async Task Add(ActionEventArgs<Content> args)
        {
            _navigationManager.NavigateTo($"/administration/admintasks/addpage");
       }


        private async Task BeginEdit(ActionEventArgs<Content> args)
        {
            content = args.Data;
            await Grid.EndEditAsync();
            _navigationManager.NavigateTo($"/administration/edit/editcontent/{content.LocationId}/{content.Name}");
        }







        private async Task RenamePage()
        {
            await Grid.EndEditAsync();
            List<Content>? records = await Grid.GetSelectedRecordsAsync();

            if (records != null && records.Count > 0)
            {
                var content = records[0];
                _navigationManager.NavigateTo($"/administration/admintasks/renamepage/{content.LocationId}/{content.Name}");
            }
        }



        protected void DataBound()
        {
            if (Pages.Count == 0) RecordText = "No Page records found";
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
                FileName = "Pages" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Pages " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Pages " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }


    }
}

