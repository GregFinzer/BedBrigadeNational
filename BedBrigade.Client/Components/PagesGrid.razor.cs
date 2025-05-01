using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using BedBrigade.Data.Services;
using Action = Syncfusion.Blazor.Grids.Action;

using Serilog;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Syncfusion.Blazor.Popups;
using ContentType = BedBrigade.Common.Enums.ContentType;


namespace BedBrigade.Client.Components
{
    public partial class PagesGrid : ComponentBase
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

        // text editor

        private SfDialog DialogInstance;
        private string EditableText;
        private bool ShowDialog = false;
        private string CurrentLocationName { get; set; }
        private string TextDialogHeading { get; set; }

        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
            Identity = _svcAuth.CurrentUser;

            var userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Manage Pages Page");

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

            //TODO:  Refactor
            bool isNationalAdmin = await _svcUser.IsUserNationalAdmin();
            if (isNationalAdmin)
            {
                var allResult = await _svcContent.GetAllAsync();

                if (allResult.Success)
                {
                    Pages = allResult.Data.ToList();
                }
            }
            else
            {
                int userLocationId = await _svcUser.GetUserLocationId();
                var contactUsResult = await _svcContent.GetAllForLocationAsync(userLocationId);
                if (contactUsResult.Success)
                {
                    Pages = contactUsResult.Data.ToList();
                }
            }

            var locResult = await _svcLocation.GetAllAsync();
            if (locResult.Success)
            {
                Locations = locResult.Data;
            }

            ContentTypes = EnumHelper.GetContentTypeItems();
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
        /// On loading of the Grid get the user grid persisted data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            string userName = await _svcUser.GetUserName();
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
            _state = await Grid.GetPersistData();
            string userName = await _svcUser.GetUserName();
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
                        FileUtil.DeleteDirectory(folderPath);
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
            PdfExportProperties ExportProperties = new PdfExportProperties
            {
                FileName = Defaults.PagesDirectory + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = Defaults.PagesDirectory + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = Defaults.PagesDirectory + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }

        // Text EDitor

        private void OpenTextDialog()        {          
            
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
            if (updateResult.Success)            {             

                _toastService.Success("Delivery Check List Saved", $"Content saved for location {CurrentLocationName}"); // VS 8/25/2024              
                StateHasChanged();
                await Grid.CloseEditAsync();
                await Grid.SetRowDataAsync(newContent.ContentId, newContent);
            }
            else
            {
                _toastService.Error("Error", $"Could not save content for location {CurrentLocationName}");
            }

        }


    } // class
} // Namespace

