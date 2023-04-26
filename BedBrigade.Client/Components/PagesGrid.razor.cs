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

namespace BedBrigade.Client.Components
{
    public partial class PagesGrid : ComponentBase
    {
        [Inject] private IContentService? _svcContent { get; set; }
        [Inject] private IUserService? _svcUser { get; set; }
        [Inject] private ILocationService? _svcLocation { get; set; }
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
        protected SfGrid<Content>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
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
        public EditContext formContext { get; set; }
        private string? saveUrl { get; set; }
        public string imagePath { get; private set; }
        public bool DialogPageNameVisible { get; private set; } = false;

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };

        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState.GetAuthenticationStateAsync();
            Identity = authState.User;
            if (Identity.HasRole("National Admin, Location Admin, Location Author, National Author"))
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
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.HasRole("National Admin, Location Admin, Location Author, National Author"))
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
        private async Task DialogPageNewOnClickHandler()
        {
            var found = await _svcContent.GetAsync(content.Name);
            if (found.Success)
            {
                ToastTitle = "Page Name Error";
                ToastContent = $"Page {content.Name} already exists!";
                await ToastObj.ShowAsync();
                return;
            }
            DialogPageNameVisible = false;
            var locationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0");
            var locResult = await _svcLocation.GetAsync(locationId);
            string locationRoute = string.Empty;
            if (locResult.Success)
            {
                locationRoute = locResult.Data.Route;
                var locationName = locResult.Data.Name;
            }

            saveUrl = $"api/image/save/{locationId}/{content.Name}";
            imagePath = $"media/{locationRoute}/pages/{content.Name}/";

            _nm.NavigateTo($"/administration/admintasks/addpage/{@saveUrl}");
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if(args.Item.Text == "Add")
            {
                DialogPageNameVisible = true;
                //saveUrl = "api/image/save/5/Test4";
                //_nm.NavigateTo($"/administration/admintasks/addpage/{@saveUrl}");
                args.Cancel = true;

            }
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
                    }
                    else
                    {
                        args.Cancel = true;
                    }

                }
                catch (Exception ex) 
                {
                    args.Cancel = true;
                    reason = ex.Message;

                }

                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

            }
        }

        private void Add()
        {
            HeaderTitle = "Add Page";
            ButtonTitle = "Add Page";
        }

        private async Task Save(ActionEventArgs<Content> args)
        {
            Content Page = args.Data;
            if (Page.ContentId != 0)
            {
                //Update Content  Record
                var updateResult = await _svcContent.UpdateAsync(Page);
                ToastTitle = "Update Page";
                if (updateResult.Success)
                {
                    ToastContent = "Page Updated Successfully!";
                }
                else
                {
                    ToastContent = "Unable to update location!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            else
            {

                // new Page
                var result = await _svcContent.CreateAsync(Page);
                if (result.Success)
                {
                    Content Content = result.Data;
                }
                ToastTitle = "Create Page";
                if (Page.ContentId != 0)
                {
                    ToastContent = "Page Created Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save Page!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            await Grid.CallStateHasChangedAsync();
            await Grid.Refresh();
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update Page";
            ButtonTitle = "Update";
        }

        protected async Task Save(Content page)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected async Task DataBound()
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

