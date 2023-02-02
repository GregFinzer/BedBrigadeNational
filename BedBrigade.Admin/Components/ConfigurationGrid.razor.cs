using BedBrigade.Admin.Services;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using System.Security.Principal;
using static BedBrigade.Shared.Common;
using Action = Syncfusion.Blazor.Grids.Action;
using Configuration = BedBrigade.Shared.Configuration;

namespace BedBrigade.Admin.Components
{
    public partial class ConfigurationGrid : ComponentBase
    {
        [Inject] private IConfigurationService? _svcConfiguration { get; set; }
        [Inject] private IUserService? _svcUser { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
        protected IEnumerable<Configuration>? ConfigRecs { get; set; }
        protected SfGrid<Configuration>? Grid { get; set; }
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
        protected int ToastTimeout { get; set; }

        protected string? RecordText { get; set; } = "Loading Configuration ...";
        protected string? Hide { get; private set; } = "true";

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
            if (Identity.IsInRole("Admin"))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }

            var result = await _svcConfiguration.GetAllAsync();
            if (result.Success)
            {
                ConfigRecs = result.Data.ToList();
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.IsInRole("Admin"))
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
            var result = await _svcUser.GetPersistAsync(PersistGrid.Configuration);
            if (result.Success)
            {
                await Grid.SetPersistData(_state);
            }
        }

        protected async Task OnDestroyed()
        {
            _state = await Grid.GetPersistData();
            await _svcUser.SavePersistAsync(new Persist { GridId = (int)PersistGrid.Configuration, UserState = _state });
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistData();
                _state = await Grid.GetPersistData();
                await _svcUser.SavePersistAsync(new Persist { GridId = (int)PersistGrid.Configuration, UserState = _state });
                return;
            }

            if (args.Item.Text == "Pdf Export")
            {
                PdfExport();
            }
            if (args.Item.Text == "Excel Export")
            {
                ExcelExport();
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
                    if (Identity.IsInRole("Admin"))
                    {
                        await NewMethod(args);
                    }
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

        private async Task NewMethod(ActionEventArgs<Configuration> args)
        {
            List<Configuration> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcConfiguration.DeleteConfigAsync(rec.ConfigurationKey);
                ToastTitle = "Delete Configuration";
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    ToastContent = $"Unable to Delete. Configuration is in use.";
                    args.Cancel = true;
                }
                ToastTimeout = 6000;
                await ToastObj.Show();

            }
        }

        private void Add()
        {
            HeaderTitle = "Add Need";
            ButtonTitle = "Add Need";
            addNeedDisplay = "display;";
            editNeedDisplay = "none;";
        }

        private async Task Save(ActionEventArgs<Configuration> args)
        {
            Configuration Configuration = args.Data;
            if (string.IsNullOrEmpty(Configuration.ConfigurationKey))
            {
                //Update Configuration Record
                var updateResult = await _svcConfiguration.UpdateAsync(Configuration);
                ToastTitle = "Update Configuration";
                if (updateResult.Success)
                {
                    ToastContent = "Configuration Updated Successfully!";
                }
                else
                {
                    ToastContent = "Unable to update Care Need!";
                }
                await ToastObj.Show();
            }
            else
            {
                // new Configuration
                var createResult = await _svcConfiguration.CreateConfigAsync(Configuration);
                if (createResult.Success)
                {
                    Configuration.ConfigurationKey = createResult.Data;
                }
                ToastObj.Title = "Create Configuration";
                if (string.IsNullOrEmpty(Configuration.ConfigurationKey))
                {
                    ToastContent = "Care Need Created Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save Care Need!";
                }
                ToastObj.Show();
            }
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update Care Need";
            ButtonTitle = "Update Care Need";
            addNeedDisplay = "display;";
            editNeedDisplay = "display;";
        }

        protected async Task Save(Configuration need)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected async Task DataBound()
        {
            if (ConfigRecs.ToList().Count == 0) RecordText = "No configurations found";
            //await Grid.AutoFitColumns();
        }

        protected async Task PdfExport()
        {
            PdfExportProperties ExportProperties = new PdfExportProperties
            {
                FileName = "Configuration" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Configuration " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Configuration " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }


    }
}

