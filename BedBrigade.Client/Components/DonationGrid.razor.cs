using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;

namespace BedBrigade.Client.Components
{
    public partial class DonationGrid : ComponentBase
    {
        [Inject] private IDonationService? _svcDonation { get; set; }
        [Inject] private IUserService? _svcUser { get; set; }
        [Inject] private ILocationService? _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
        protected List<Donation>? Donations { get; set; }
        protected List<Location>? Locations { get; set; }
        protected SfGrid<Donation>? Grid { get; set; }
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

        protected string? RecordText { get; set; } = "Loading Donations ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }

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
            if (Identity.IsInRole("National Admin") || Identity.IsInRole("Location Admin"))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }

            var result = await _svcDonation.GetAllAsync();
            if (result.Success)
            {
                Donations = result.Data.ToList();
            }
            var locationResult = await _svcLocation.GetAllAsync();
            if (locationResult.Success)
            {
                Locations = locationResult.Data.ToList();
            }

        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.IsInRole("National Admin") || Identity.IsInRole("Location Admin"))
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
            var result = await _svcUser.GetPersistAsync(Common.Common.PersistGrid.User);
            if (result.Success)
            {
                await Grid.SetPersistData(_state);
            }
        }

        protected async Task OnDestroyed()
        {
            _state = await Grid.GetPersistData();
            await _svcUser.SavePersistAsync(new Persist { GridId = (int)Common.Common.PersistGrid.Donation, UserState = _state });
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistData();
                _state = await Grid.GetPersistData();
                await _svcUser.SavePersistAsync(new Persist { GridId = (int)Common.Common.PersistGrid.Donation, UserState = _state });
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

        public async Task OnActionBegin(ActionEventArgs<Donation> args)
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

        private async Task Delete(ActionEventArgs<Donation> args)
        {
            List<Donation> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcDonation.DeleteAsync(rec.DonationId);
                ToastTitle = "Delete Donation";
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    ToastContent = $"Unable to Delete. Donation is in use.";
                    args.Cancel = true;
                }
                ToastTimeout = 6000;
                await ToastObj.Show();

            }
        }

        private void Add()
        {
            HeaderTitle = "Add Donation";
            ButtonTitle = "Add Donation";
        }

        private async Task Save(ActionEventArgs<Donation> args)
        {
            Donation Donation = args.Data;
            if (Donation.DonationId != 0)
            {
                //Update Donation Record
                var updateResult = await _svcDonation.UpdateAsync(Donation);
                ToastTitle = "Update Donation";
                if (updateResult.Success)
                {
                    ToastContent = "Donation Updated Successfully!";
                }
                else
                {
                    ToastContent = "Unable to update location!";
                }
                await ToastObj.Show();
            }
            else
            {
                // new Donation
                var result = await _svcDonation.CreateAsync(Donation);
                if (result.Success)
                {
                    Donation location = result.Data;
                }
                ToastTitle = "Create Donation";
                if (Donation.DonationId == 0)
                {
                    ToastContent = "Donation Created Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save Donation!";
                }
                await ToastObj.Show();
            }
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update Donation";
            ButtonTitle = "Update";
        }

        protected async Task Save(Donation location)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected async Task DataBound()
        {
            if (Donations.Count == 0) RecordText = "No Donation records found";
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
                FileName = "Donation" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Donation " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Donation " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }


    }
}

