using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;
using static BedBrigade.Common.Common;
using Microsoft.AspNetCore.Components.Forms;
using Serilog;
using ContentType = BedBrigade.Common.Common.ContentType;


namespace BedBrigade.Client.Components
{
    public partial class PagesGrid : ComponentBase
    {
        [Inject] private IContentService? _svcContent { get; set; }
        [Inject] private IUserService? _svcUser { get; set; }
        [Inject] private ILocationService? _svcLocation { get; set; }
        [Inject] private IWebHostEnvironment _svcEnv { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private NavigationManager? _nm { get; set; }

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
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
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
                var item = Locations.Single(r => r.LocationId == 0);
                if (item != null)
                {
                    Locations.Remove(item);
                }
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
            var result = await _svcUser.GetPersistAsync(new Persist { GridId = (int)PersistGrid.Pages, UserState = await Grid.GetPersistDataAsync() });
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
            var result = await _svcUser.SavePersistAsync(new Persist { GridId = (int)PersistGrid.Pages, UserState = _state });
            if (!result.Success)
            {
                //Log the results
            }

        }

        /// <summary>
        /// Create a new content of a specific type
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        //private async Task AddContentHandler()
        //{
        //    AddContentVisible = false;
        //    switch(content.ContentType)
        //    {
        //        case ContentType.Body:
        //            content.Name = "NewPage";
        //            break;
        //        case ContentType.Header:
        //            content.Name = "Header";
        //            break;
        //        case ContentType.Footer:
        //            content.Name = "Footer";
        //            break;
        //        case ContentType.Home:
        //            content.Name = "Home";
        //            break;
        //    }
        //    var totalOfType = 1;
        //    var result = await _svcContent.GetAllAsync(content.ContentType, content.LocationId);
        //    if (result.Success)
        //    {
        //        totalOfType = result.Data.Count();
        //    }

        //    content.Name = $"{content.Name}_{content.LocationId}_{totalOfType++}";
        //    saveUrl = $"api/image/save/{content.LocationId}/{content.Name}";
        //    imagePath = $"media/Templates/pages/{content.Name}/";

        //    _nm.NavigateTo($"/administration/admintasks/addpage/{@saveUrl}");
        //}


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {

            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistData();
                _state = await Grid.GetPersistData();
                await _svcUser.SavePersistAsync(new Persist { GridId = (int)Common.Common.PersistGrid.Pages, UserState = _state });
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
            //content = new Content();
            //AddContentVisible = true;
            var user = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            saveUrl = $"api/image/save/temp-work/{user}/Page-to-Add";
            _nm.NavigateTo($"/administration/admintasks/addpage/{@saveUrl}");
            return;
       }


        private async Task BeginEdit(ActionEventArgs<Content> args)
        {
            content = args.Data;
            await Grid.EndEditAsync();
            saveUrl = $"api/image/save/{content.LocationId}/{content.Name}";
            _nm.NavigateTo($"/administration/edit/editpage/{saveUrl}");
        }

        protected async Task Save(Content page)
        {
            CurrentValues = Pages.Find(p => p.ContentId == page.ContentId);
            CheckValues(page, CurrentValues);
            await Grid.EndEdit();
        }

        /// <summary>
        /// Method to compare properties between to objects
        /// </summary>
        /// <param name="page">Object with possible changed values</param>
        /// <param name="currentValues">Object with values before possible change</param>
        /// <returns>True if a value was changed</returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool CheckValues(Content page, Content values)
        {
            if(page.Title != values.Title)
            {
                ChangeTitle(values.Name, page.Title);
            }
            if (page.HeaderMediaId != values.HeaderMediaId)
            {
                ChangeMediaId(values.Name, values.HeaderMediaId, page.HeaderMediaId);
            }
            if (page.FooterMediaId != values.FooterMediaId)
            {
                ChangeMediaId(values.Name, values.FooterMediaId, page.FooterMediaId);
            }
            if (page.LeftMediaId != values.LeftMediaId)
            {
                ChangeMediaId(values.Name, values.LeftMediaId, page.LeftMediaId);
            }
            if (page.MiddleMediaId != values.MiddleMediaId)
            {
                ChangeMediaId(values.Name, values.MiddleMediaId, page.MiddleMediaId);
            }
            if (page.RightMediaId != values.RightMediaId)
            {
                ChangeMediaId(values.Name, values.RightMediaId, page.RightMediaId);
            }
            // This if must be the last
            if (page.Name != values.Name)
            {
                RenamePage(page.Name, values.Name, values.LocationId);
            }
            return true;
        }

        private void ChangeMediaId(string? headerMediaId1, string name, string? headerMediaId2)
        {
            return;
        }

        private void ChangeTitle(string name, string title)
        {
            return;
        }

        private void RenamePage(string? newName, string? oldName, int LocationId)
        {
            var locationRoute = Locations.Find(l => l.LocationId == LocationId).Route;
            var oldPath = $"{_svcEnv.ContentRootPath}/wwwroot/media{locationRoute}/pages/{oldName}";
            var newPath = $"{_svcEnv.ContentRootPath}/wwwroot/media{locationRoute}/pages/{newName}"; 
            Directory.Move(oldPath, newPath);
            Log.Information($"Renamed Page Folder at {oldPath} to {newPath}");
        }

        protected async Task EditPage(Content page)
        {
            await Grid.EndEditAsync();
            saveUrl = $"api/image/save/{page.LocationId}/{page.Name}";
            _nm.NavigateTo($"/administration/edit/editpage/{saveUrl}");
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
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

