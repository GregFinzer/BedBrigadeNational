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
using BedBrigade.Client.Pages.Administration.Manage;
using System.Data;
using BedBrigade.Client.Pages.Home;

namespace BedBrigade.Client.Components
{
    public partial class BedRequestGrid : ComponentBase
    {
        [Inject] private IBedRequestService? _svcBedRequest { get; set; }
        [Inject] private IUserService? _svcUser { get; set; }
        [Inject] private ILocationService _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
        protected List<BedRequest>? BedRequests { get; set; }
        protected List<Location>? Locations { get; set; }
        protected SfGrid<BedRequest>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected BedRequest BedRequest { get; set; } = new BedRequest();
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? addNeedDisplay { get; private set; }
        protected string? editNeedDisplay { get; private set; }
        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; } = 3000;
        protected bool OnlyRead { get; set; } = false;

        protected string? RecordText { get; set; } = "Loading BedRequests ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }
        public List<EnumItem> BedRequestStatuses { get; private set; }

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
            if (Identity.HasRole("National Admin, Location Admin, Location Scheduler"))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }

            if(Identity.IsInRole("Location Admin") || Identity.IsInRole("Location Scheduler"))
            {
                OnlyRead = true;
            }

            var bedRequestResult = await _svcBedRequest.GetAllAsync();
            if (bedRequestResult.Success)
            {
                BedRequests = bedRequestResult.Data.ToList();
            }

            var locationResult = await _svcLocation.GetAllAsync();
            if (locationResult.Success)
            {
                Locations = locationResult.Data.ToList();
            }

            BedRequestStatuses = GetBedRequestStatusItems();
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.IsInRole("National Admin") || Identity.IsInRole("Location Admin") || Identity.IsInRole("Location Scheduler"))
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
            var result = await _svcUser.GetPersistAsync(new Persist { GridId = (int) PersistGrid.BedRequest, UserState = await Grid.GetPersistData() });
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
            var result = await _svcUser.SavePersistAsync(new Persist { GridId = (int)PersistGrid.BedRequest, UserState = _state });
            if(!result.Success)
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
                await _svcUser.SavePersistAsync(new Persist { GridId = (int)Common.Common.PersistGrid.BedRequest, UserState = _state });
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

        public async Task OnActionBegin(ActionEventArgs<BedRequest> args)
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

        private async Task Delete(ActionEventArgs<BedRequest> args)
        {
            List<BedRequest> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcBedRequest.DeleteAsync(rec.BedRequestId);
                ToastTitle = "Delete BedRequest";
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    ToastContent = $"Unable to Delete. BedRequest is in use.";
                    args.Cancel = true;
                }
                ToastTimeout = 6000;
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

            }
        }

        private void Add()
        {
            HeaderTitle = "Add BedRequest";
            ButtonTitle = "Add BedRequest";
            BedRequest.LocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
        }


        private async Task Save(ActionEventArgs<BedRequest> args)
        {
            BedRequest BedRequest = args.Data;
            BedRequest.Phone = BedRequest.Phone.FormatPhoneNumber();
            if (BedRequest.BedRequestId != 0)
            {
                //Update BedRequest Record
                var updateResult = await _svcBedRequest.UpdateAsync(BedRequest);
                ToastTitle = "Update BedRequest";
                if (updateResult.Success)
                {
                    ToastContent = "BedRequest Updated Successfully!";
                }
                else
                {
                    ToastContent = "Unable to update BedRequest!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            else
            {
                // new BedRequest
                var result = await _svcBedRequest.CreateAsync(BedRequest);
                if (result.Success)
                {
                    BedRequest = result.Data;
                }
                ToastTitle = "Create BedRequest";
                if (BedRequest.BedRequestId != 0)
                {
                    ToastContent = "BedRequest Created Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save BedRequest!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            await Grid.Refresh();
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update BedRequest";
            ButtonTitle = "Update";
        }

        protected async Task Save(BedRequest BedRequest)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected async Task DataBound()
        {
            if (BedRequests.Count == 0) RecordText = "No BedRequest records found";
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
                FileName = "BedRequest" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "BedRequest " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "BedRequest " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }


    }
}

