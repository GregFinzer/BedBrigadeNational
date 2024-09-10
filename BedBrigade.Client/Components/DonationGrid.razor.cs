using BedBrigade.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
//using Org.BouncyCastle.Asn1.Cms;
//using Org.BouncyCastle.Asn1.X509;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Linq;
using System.Security.Claims;
using BedBrigade.Data.Services;
using Serilog;

using static System.Net.Mime.MediaTypeNames;
using Action = Syncfusion.Blazor.Grids.Action;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Client.Components.Pages.Administration.Manage;
using BedBrigade.Common.Logic;

namespace BedBrigade.Client.Components
{
    public partial class DonationGrid : ComponentBase
    {
        [Inject] private IDonationDataService? _svcDonation { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private const string SendTaxForm = "Send Tax Form";

        private ClaimsPrincipal? Identity { get; set; }
        protected List<Donation>? Donations { get; set; }
        protected List<Location>? Locations { get; set; }
        protected SfGrid<Donation>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<object>? ContextMenu;
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? addNeedDisplay { get; private set; }
        protected string? editNeedDisplay { get; private set; }
        protected string[] groupColumns = new string[] { "LocationId" };
        protected string? RecordText { get; set; } = "Loading Donations ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }
        public bool TaxIsVisible { get; private set; }
        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "400px" };
        private Donation Donation = new Donation();

        /// <summary>
        /// Setup the Donation Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState.GetAuthenticationStateAsync();
            Identity = authState.User;

            var userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Manage Donations Page");

            SetupAccess();
            await LoadDonations();
            await LoadLocations();

            var query = from donation in Donations
                    where donation.TaxFormSent == false
                    select new ListItem { Email = donation.Email, Name = donation.FullName, Amount= donation.Amount };                      
            NotSent = query.ToList();

        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.IsInRole(RoleNames.LocationAdmin) || Identity.IsInRole(RoleNames.LocationTreasurer))
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

        private async Task LoadLocations()
        {
            var locationResult = await _svcLocation.GetAllAsync();
            if (locationResult.Success)
            {
                Locations = locationResult.Data.ToList();
                var item = Locations.Single(r => r.LocationId == (int)LocationNumber.National);
                if (item != null)
                {
                    Locations.Remove(item);
                }
            }
        }

        private async Task LoadDonations()
        {
            bool isNationalAdmin = await _svcUser.IsUserNationalAdmin();
            if (isNationalAdmin)
            {
                var allResult = await _svcDonation.GetAllAsync();

                if (allResult.Success)
                {
                    Donations = allResult.Data.ToList();
                }
            }
            else
            {
                int userLocationId = await _svcUser.GetUserLocationId();
                var contactUsResult = await _svcDonation.GetAllForLocationAsync(userLocationId);
                if (contactUsResult.Success)
                {
                    Donations = contactUsResult.Data.ToList();
                }
            }
        }

        private void SetupAccess()
        {
            if (Identity.IsInRole(RoleNames.LocationAdmin) || Identity.IsInRole(RoleNames.LocationTreasurer))
            {
                ToolBar = new List<string> { SendTaxForm, "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<object> {"Edit", 
                    "Delete",
                    new ContextMenuItemModel {Id = "Tax", Text = SendTaxForm, Target = ".e-content" },
                    FirstPage,
                    NextPage,
                    PrevPage,
                    LastPage,
                    "AutoFit",
                    "AutoFitAll",
                    "SortAscending",
                    "SortDescending"
                };

            }
            else if (Identity.IsInRole(RoleNames.NationalAdmin))
            {
                ToolBar = new List<string> { SendTaxForm, "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<object> {
                    new ContextMenuItemModel {Id = "Tax", Text = SendTaxForm, Target = ".e-content" },
                    FirstPage,
                    NextPage,
                    PrevPage,
                    LastPage,
                    "AutoFit",
                    "AutoFitAll",
                    "SortAscending",
                    "SortDescending"
                };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<object> {
                    new ContextMenuItemModel {Id = "Tax", Text = SendTaxForm, Target = ".e-content" },
                    FirstPage,
                    NextPage,
                    PrevPage,
                    LastPage,
                    "AutoFit",
                    "AutoFitAll",
                    "SortAscending",
                    "SortDescending"
                };
            }
        }

        /// <summary>
        /// On loading of the Grid get the user grid persisted data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            string userName = await _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Donation };
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Donation, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Donation} : {result.Message}");
            }
        }

        protected void OnContextMenuClicked(ContextMenuClickEventArgs<Donation> args)
        {
            if(args.Item.Text == SendTaxForm)
            {
                TaxIsVisible = true;
            }
        }

        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if(args.Item.Text == SendTaxForm)
            {
                TaxIsVisible = true;
                return;
            }
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

        private void BeginEdit()
        {
            HeaderTitle = "Update Donation";
            ButtonTitle = "Update";
        }
        private void Add()
        {
            HeaderTitle = "Add Donation";
            ButtonTitle = "Add";
            Donation.LocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        private async Task Save(ActionEventArgs<Donation> args)
        {
            Donation donation = args.Data;
            if (donation.DonationId != 0)
            {
                //Update Record
                var updateResult = await _svcDonation.UpdateAsync(donation);
                
                if (updateResult.Success)
                {
                    _toastService.Success("Donation Updated", "Donation Updated Successfully!");
                }
                else
                {
                    _toastService.Error("Could not update donation", "Unable to update Donation!");
                }
            }
            else
            {
                // new 
                var result = await _svcDonation.CreateAsync(donation);
                if (result.Success)
                {
                    Donation = result.Data;
                }
                if (result.Success)
                {
                    _toastService.Success("Donation Added", "Donation Added Successfully!");
                }
                else
                {
                    _toastService.Error("Could not add donation", "Unable to add Donation!");
                }
            }
            await Grid.Refresh();
        }

        protected async Task Save(Donation donation)
        {
            await Grid.EndEdit();
        }

        private async Task Delete(ActionEventArgs<Donation> args)
        {
            List<Donation> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcDonation.DeleteAsync(rec.DonationId);
                
                if (deleteResult.Success)
                {
                    _toastService.Success("Donation Deleted", "Donation deleted");
                }
                else
                {
                    _toastService.Error("Unable to Delete Donation", "Donation could not be deleted");
                    args.Cancel = true;
                }
            }
        }

        protected void DataBound()
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


        #region Send Tax

        protected async Task CloseTaxDialog()
        {
            TaxIsVisible = false;

        }

        protected async Task SendTax()
        {
            foreach (var recipient in LB_Send.GetDataList())
            {
                string body = $"Dear {recipient.Name},\n\n" +
                              $"Thank you for your generous donation of {recipient.Amount.ToString("C")} to the Bed Brigade";
                //await _messageService.SendEmailAsync(recipient.Email, 
                //    "national@bedbrigade.org", 
                //    "Bed Brigade Charitable Donation", 
                //    body);
            }
            await CloseTaxDialog();
        }

        private string[] items = new string[] { "MoveTo", "MoveFrom", "MoveAllTo", "MoveAllFrom" };
        private readonly string scope1 = "scope1";
        private readonly string scope2 = "scope2";
        private readonly Dictionary<string, object> listbox1Attr = new Dictionary<string, object>
        {
        { "id", "scope1" }
        };
        private readonly Dictionary<string, object> listbox2Attr = new Dictionary<string, object>
        {
        { "id", "scope2" }
        };
        public  SfListBox<string[], ListItem> LB_NotSent;
        public  SfListBox<string[], ListItem> LB_Send;
        private List<ListItem> NotSent = new List<ListItem>();
        private List<ListItem> Send = new List<ListItem>();
        public class ListItem
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public decimal Amount { get; set; }

        }
        #endregion

    }
}

