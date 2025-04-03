using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using System.Security.Claims;
using Serilog;
using Task = System.Threading.Tasks.Task;
using BedBrigade.Common.Enums;
using BedBrigade.Common.EnumModels;
using Syncfusion.Blazor.Notifications.Internal;
using Syncfusion.Blazor.Notifications;
using Action = Syncfusion.Blazor.Grids.Action;

namespace BedBrigade.Client.Components.Pages.Administration.Manage
{
    public partial class ManageNewsletters : ComponentBase
    {
        [Inject] private INewsletterDataService _svcNewsletter { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ToastService? _toastService { get; set; }
        protected List<Newsletter>? NewsletterRecords { get; set; }
        protected SfGrid<Newsletter>? Grid { get; set; }
        private ClaimsPrincipal? Identity { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        protected List<Location> Locations { get; set; } = new List<Location>();
        protected string? _state { get; set; }
        protected string? RecordText { get; set; } = "Loading Newsletters ...";
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected bool AddMode { get; set; } = false;
        protected bool NoPaging { get; private set; }
        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };
        public bool IsLocationColumnVisible { get; set; } = false;
        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
            Identity = _svcAuth.CurrentUser;
            var userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Manage Newsletters Page");

            var locationResult = await _svcLocation.GetAllAsync();
            if (locationResult.Success)
            {
                Locations = locationResult.Data;
            }

            ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
            ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };

            bool isNationalAdmin = await _svcUser.IsUserNationalAdmin();

            //Get all records when an admin
            if (isNationalAdmin)
            {
                IsLocationColumnVisible = true;
                var result = await _svcNewsletter.GetAllAsync();
                if (result.Success)
                {
                    NewsletterRecords = result.Data;
                }
            }
            else
            {
                var locationId = await _svcUser.GetUserLocationId();
                var result = await _svcNewsletter.GetAllForLocationAsync(locationId);
                if (result.Success)
                {
                    NewsletterRecords = result.Data;
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
            string userName = await _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Newsletter };
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Newsletter, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Newsletter} : {result.Message}");
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

        public async Task OnActionBegin(ActionEventArgs<Newsletter> args)
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
            HeaderTitle = "Update Newsletter";
            ButtonTitle = "Update Newsletter";
            AddMode = false;
        }

        private async Task Delete(ActionEventArgs<Newsletter> args)
        {
            List<Newsletter> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcNewsletter.DeleteAsync(rec.NewsletterId);
                
                if (deleteResult.Success)
                {
                    _toastService.Success("Newsletter Deleted", $"Newsletter Deleted: " + rec.Name);
                }
                else
                {
                    _toastService.Success("Error", $"Newsletter could not be deleted: " + rec.Name);
                    args.Cancel = true;
                }
            }
        }

        private void Add()
        {
            HeaderTitle = "Add Newsletter";
            ButtonTitle = "Add Newsletter";
            AddMode = true;
        }

        private async Task Save(ActionEventArgs<Newsletter> args)
        {
            Newsletter newsletter = args.Data;
            if (newsletter.LocationId == 0 && !AddMode)
            {
                //Update Newsletter Record
                var updateResult = await _svcNewsletter.UpdateAsync(newsletter);
                if (updateResult.Success)
                {
                    _toastService.Success("Newsletter Updated", $"Newsletter Updated: " + newsletter.Name);
                }
                else
                {
                    _toastService.Error("Error", $"Could not update newsletter: " + newsletter.Name);
                }
            }
            else
            {
                // New Newsletter
                var createResult = await _svcNewsletter.CreateAsync(newsletter);

                if (createResult.Success)
                {
                    _toastService.Success("Newsletter Added", $"Newsletter Added: " + newsletter.Name);
                }
                else
                {
                    _toastService.Error("Error", $"Could not add newsletter: " + newsletter.Name);
                }
            }
        }
        protected async Task Save(Newsletter need)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected void DataBound()
        {
            if (NewsletterRecords.ToList().Count == 0) RecordText = "No newsletters found";
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
            PdfExportProperties ExportProperties = new PdfExportProperties
            {
                FileName = "Newsletters" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Newsletters " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Newsletters " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }

    }
}
