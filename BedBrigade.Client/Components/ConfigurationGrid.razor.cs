using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using BedBrigade.Data.Services;
using Serilog;
using Action = Syncfusion.Blazor.Grids.Action;
using static BedBrigade.Common.Logic.Common;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;

namespace BedBrigade.Client.Components
{
    public partial class ConfigurationGrid : ComponentBase
    {
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
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
        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; } = 5000;
        protected bool NoPaging { get; private set; }
        protected bool AddKey { get; set; } = false;
        protected string? RecordText { get; set; } = "Loading Configuration ...";
        protected string? Hide { get; private set; } = "true";
        public List<ConfigSectionEnumItem> ConfigSectionEnumItems { get; private set; }

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
            var userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Manage Configurations Page");

            if (Identity.IsInRole(RoleNames.NationalAdmin))
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

            ConfigSectionEnumItems = GetConfigSectionItems();
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.IsInRole(RoleNames.NationalAdmin))
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
            var result = await _svcUser.GetGridPersistance(new Persist { GridId = (int)PersistGrid.Configuration, UserState = await Grid.GetPersistData() })!;
            if (result.Success)
            {
                await Grid.SetPersistDataAsync(result.Data);
            }
        }

        /// <summary>
        /// On destoring of the grid save its current state
        /// </summary>
        /// <returns></returns>
        protected async Task OnDestroyed()
        {
            _state = await Grid.GetPersistData();
            var result = await _svcUser.SaveGridPersistance(new Persist { GridId = (int)PersistGrid.Configuration, UserState = _state });
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
                await _svcUser.SaveGridPersistance(new Persist { GridId = (int)PersistGrid.Configuration, UserState = _state });
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
            List<Configuration> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcConfiguration.DeleteAsync(rec.ConfigurationKey);
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
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

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
            Configuration Configuration = args.Data;
            if (!string.IsNullOrEmpty(Configuration.ConfigurationKey) && !AddKey )
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
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            else
            {
                // new Configuration
                var createResult = await _svcConfiguration.CreateAsync(Configuration);
                if (createResult.Success)
                {
                    Configuration config = createResult.Data;
                }
                ToastTitle = "Create Configuration";
                if (createResult.Success)
                {
                    ToastContent = "Configuration Created Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save configuration!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
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

