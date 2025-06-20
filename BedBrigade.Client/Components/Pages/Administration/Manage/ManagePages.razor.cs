using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.Grids;
using Action = Syncfusion.Blazor.Grids.Action;
using ContentType = BedBrigade.Common.Enums.ContentType;


namespace BedBrigade.Client.Components.Pages.Administration.Manage
{
    public partial class ManagePages : ComponentBase
    {
        [Inject] private IContentDataService? _svcContent { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IWebHostEnvironment _svcEnv { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private NavigationManager? _navigationManager { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        protected List<Content>? Pages { get; set; }
        protected Content content { get; set; }
        protected Content CurrentValues { get; set; }
        protected SfGrid<Content>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected string[] groupColumns = new string[] { "LocationId" };
        protected string? _state { get; set; }
        protected string? RecordText { get; set; } = "Loading Pages ...";
        public bool NoPaging { get; private set; }
        public List<Location> Locations { get; private set; }


        [Parameter]
        public string? ContentTypeString { get; set; }

        public List<ContentTypeEnumItem> ContentTypes { get; private set; }


        // text editor

        private string EditableText;
        private bool ShowDialog = false;
        private string CurrentLocationName { get; set; }
        private string TextDialogHeading { get; set; }

        private ContentType _contentType;
        private string _subdirectory;

        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
        }

        protected override async Task OnParametersSetAsync()
        {
            await InitializeAndLoad();
        }

        private async Task InitializeAndLoad()
        {
            try
            {
                Log.Information($"{_svcAuth.UserName} went to the Manage Pages with a Content Type of {ContentTypeString}");
                _contentType = Enum.Parse<ContentType>(ContentTypeString);

                _subdirectory = BlogTypes.ValidBlogTypes.Contains(_contentType) ? _contentType.ToString() : "pages";

                if (_svcAuth.UserHasRole(RoleNames.CanManagePages))
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

                await LoadData();

                if (Locations == null)
                {
                    var locResult = await _svcLocation.GetAllAsync();
                    if (locResult.Success && locResult.Data != null)
                    {
                        Locations = locResult.Data;
                    }
                    else
                    {
                        Log.Error($"ManagePages, Error loading locations: {locResult.Message}");
                        _toastService.Error("Error Loading Locations", $"Could not load locations: {locResult.Message}");
                    }
                }

                ContentTypes = EnumHelper.GetContentTypeItems();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ManagePages component");
                _toastService.Error("Error Initializing", $"An error occurred while initializing the page: {ex.Message}");
            }
        }

        private async Task LoadData()
        {
            bool isNationalAdmin = _svcUser.IsUserNationalAdmin();
            if (isNationalAdmin)
            {
                if (BlogTypes.ValidBlogTypes.Contains(_contentType))
                {
                    var contentTypeResult = await _svcContent.GetByContentType(_contentType);
                    if (contentTypeResult.Success && contentTypeResult.Data != null)
                    {
                        Pages = contentTypeResult.Data.ToList();
                        return;
                    }
                }

                var allResult = await _svcContent.GetAllExceptBlogTypes();

                if (allResult.Success && allResult.Data != null)
                {
                    Pages = allResult.Data.ToList();
                    return;
                }
            }

            int userLocationId = _svcUser.GetUserLocationId();
            if (BlogTypes.ValidBlogTypes.Contains(_contentType))
            {
                var locationContentTypeResult = await _svcContent.GetByLocationContentType(userLocationId, _contentType);
                if (locationContentTypeResult.Success && locationContentTypeResult.Data != null)
                {
                    Pages = locationContentTypeResult.Data.ToList();
                    return;
                }
            }
                
            var locationResult = await _svcContent.GetForLocationExceptBlogTypes(userLocationId);
            if (locationResult.Success && locationResult.Data != null)
            {
                Pages = locationResult.Data.ToList();
            }
            else
            {
                Log.Error($"ManagePages, Error loading pages for location {userLocationId}: {locationResult.Message}");
                _toastService.Error("Error Loading Pages", $"Could not load pages: {locationResult.Message}");
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (_svcAuth.UserHasRole(RoleNames.CanManagePages))
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Pages };
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
            _state = await Grid.GetPersistDataAsync();
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Pages, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Pages} : {result.Message}");
            }
        }




        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {

            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistDataAsync();
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
                    switch (args.Data.ContentType)
                    {
                        case ContentType.DeliveryCheckList:
                        case ContentType.EmailTaxForm:
                        case ContentType.BedRequestConfirmationForm:
                        case ContentType.SignUpEmailConfirmationForm:
                        case ContentType.NewsletterForm:
                            CurrentValues = args.Data;
                            await SetLocationName();
                            TextDialogHeading = "Edit " + EnumHelper.GetEnumDescription(args.Data.ContentType);
                            OpenTextDialog();
                            break;
                        default:
                            await BeginEdit(args);
                            break;
                    }

                    break;
            }
        }

        private async Task Delete(ActionEventArgs<Content> args)
        {
            List<Content> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                try
                {
                    var deleteResult = await _svcContent.DeleteAsync(rec.ContentId);
                    if (deleteResult.Success)
                    {
                        _toastService.Success("Success", $"Page {rec.Name} deleted successfully.");
                        var locationRoute = Locations.Find(l => l.LocationId == rec.LocationId).Route;
                        var folderPath = $"{_svcEnv.ContentRootPath}/wwwroot/media{locationRoute}/{_subdirectory}/{rec.Name}";
                        FileUtil.DeleteDirectory(folderPath);
                        Log.Information($"Deleted Page Folder at {folderPath}");
                    }
                    else
                    {
                        Log.Error("ManagePages, Could not delete: " + deleteResult.Message);
                        _toastService.Error("Error Deleting", $"Could not delete page {rec.Name}: {deleteResult.Message}");
                        args.Cancel = true;
                        return;
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ManagePages, Could not delete");
                    _toastService.Error("Error Deleting", $"Could not delete page {rec.Name}: {ex.Message}");
                    args.Cancel = true;
                    return;
                }
            }
        }

        private async Task Add(ActionEventArgs<Content> args)
        {
            if (BlogTypes.ValidBlogTypes.Contains(_contentType))
            {
                _navigationManager.NavigateTo($"/administration/admintasks/addpage/{_contentType}");
                return;
            }
            _navigationManager.NavigateTo($"/administration/admintasks/addpage/Body");
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
            PdfExportProperties exportProperties = new PdfExportProperties
            {
                FileName = Defaults.PagesDirectory + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(exportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties exportProperties = new ExcelExportProperties
            {
                FileName = Defaults.PagesDirectory + DateTime.Now.ToShortDateString() + ".xlsx"

            };

            await Grid.ExportToExcelAsync(exportProperties);
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties exportProperties = new ExcelExportProperties
            {
                FileName = Defaults.PagesDirectory + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.ExportToCsvAsync(exportProperties);
        }

        // Text EDitor

        private void OpenTextDialog()
        {

            EditableText = StringUtil.IsNull(CurrentValues.ContentHtml, "");
            ShowDialog = true;
        }

        private async Task SaveText()
        {
            // Save the edited text back to the database
            CurrentValues.ContentHtml = EditableText;
            await UpdatePageContent(CurrentValues);
            ShowDialog = false;
        }
        private async Task SetLocationName()
        {
            var locationResult = await _svcLocation.GetByIdAsync(CurrentValues.LocationId);
            if (locationResult.Success && locationResult.Data != null)
            {
                CurrentLocationName = locationResult.Data.Name;
            }
        } // Get Location


        private void CloseTextDialog()
        {
            ShowDialog = false;
        }

        private async Task UpdatePageContent(Content newContent)
        {
            //Update Content  Record
            var updateResult = await _svcContent.UpdateAsync(newContent);
            if (updateResult.Success)
            {

                _toastService.Success("Content Updated", $"Content saved for location {CurrentLocationName}"); // VS 8/25/2024              
                StateHasChanged();
                await Grid.CloseEditAsync();
                await Grid.SetRowDataAsync(newContent.ContentId, newContent);
            }
            else
            {
                _toastService.Error("Error Updating", $"Could not update content for location {CurrentLocationName}");
            }

        }


    } // class
} // Namespace

