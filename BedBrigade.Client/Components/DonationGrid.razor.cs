using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Linq;
using System.Security.Claims;
using static BedBrigade.Common.Common;
using static System.Net.Mime.MediaTypeNames;
using Action = Syncfusion.Blazor.Grids.Action;

namespace BedBrigade.Client.Components
{
    public partial class DonationGrid : ComponentBase
    {
        [Inject] private IDonationService? _svcDonation { get; set; }
        [Inject] private IUserService? _svcUser { get; set; }
        [Inject] private ILocationService? _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private IMessageService? _messageService { get; set; }

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
        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; }
        protected string[] groupColumns = new string[] { "LocationId" };
        protected string? RecordText { get; set; } = "Loading Donations ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }
        public bool TaxIsVisible { get; private set; }

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };

        /// <summary>
        /// Setup the Donation Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState.GetAuthenticationStateAsync();
            Identity = authState.User;
            if (Identity.IsInRole("National Admin") || Identity.IsInRole("Location Admin") || Identity.IsInRole("Location Treasure"))
            {
                ToolBar = new List<string> { SendTaxForm, "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
            }
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

            var query = from donation in Donations
                        where donation.TaxFormSent == false
                        select new ListItem { Email = donation.Email, Name = donation.FullName, Amount= donation.Amount };
                       

            NotSent = query.ToList();

        }

        /// <summary>
        /// On loading of the Grid get the user grid persited data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            var result = await _svcUser.GetPersistAsync(new Persist { GridId = (int)PersistGrid.Donation, UserState = await Grid.GetPersistDataAsync() });
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
            var result = await _svcUser.SavePersistAsync(new Persist { GridId = (int)PersistGrid.Donation, UserState = _state });
            if (!result.Success)
            {
                //Log the results
            }

        }

        protected async Task OnContextMenuClicked(ContextMenuClickEventArgs<Donation> args)
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
            }

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


        #region Send Tax

        protected async Task CloseTaxDialog()
        {
            TaxIsVisible = false;
        }

        protected async Task SendTax()
        {
            foreach(var recipient in LB_Send.GetDataList() )
            {
                await _messageService.SendEmailAsync(recipient.Email, "national@bedbrigade.org", "Bed Brigade Charitable Donation", "TaxDonation", new { FullName = recipient.Name, Amount = recipient.Amount, Email = recipient.Email });
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

