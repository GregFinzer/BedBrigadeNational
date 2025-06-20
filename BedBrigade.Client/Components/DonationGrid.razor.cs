using System.Collections;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using BedBrigade.Data.Services;
using Serilog;
using Action = Syncfusion.Blazor.Grids.Action;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components
{
    public partial class DonationGrid : ComponentBase
    {
        [Inject] private IDonationDataService? _svcDonation { get; set; }
        [Inject] private IDonationCampaignDataService? _svcDonationCampaign { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private IEmailBuilderService _svcEmailBuilder { get; set; }
        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private const string SendTaxForm = "Send Tax Form";


        protected List<Donation>? Donations { get; set; }
        protected List<Location>? Locations { get; set; }
        protected List<DonationCampaign>? DonationCampaigns { get; set; } 
        protected SfGrid<Donation>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<object>? ContextMenu;
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }

        protected string[] groupColumns = new string[] { "LocationId" };
        protected string? RecordText { get; set; } = "Loading Donations ...";

        public bool NoPaging { get; private set; }
        public bool TaxIsVisible { get; private set; }
        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "400px" };
        private Donation Donation = new Donation();
        protected decimal FilteredSum { get; set; }

        /// <summary>
        /// Setup the Donation Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                Log.Information($"{_svcAuth.UserName} went to the Manage Donations Page");

                SetupAccess();
                await LoadDonations();
                await LoadLocations();
                await LoadDonationCampaigns();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing DonationGrid component");
                _toastService.Error("Error loading donations", "An error occurred while loading donations. Please try again later.");
            }
            finally
            {
                if (Donations != null && Donations.Count > 0)
                {
                    RecordText = $"{Donations.Count} Donation records found";
                }
                else
                {
                    RecordText = "No Donation records found";
                }
            }
        }

        private async Task UpdateFilteredSum()
        {
            var groups = (IEnumerable)await Grid.GetFilteredRecordsAsync();
            FilteredSum = 0;
            foreach (var groupObject in groups)
            {
                var groupProperties = groupObject.GetType().GetProperties();
                var itemsProperty = groupProperties.FirstOrDefault(p => p.Name == "Items");

                if (itemsProperty != null)
                {
                    var items = (IEnumerable<Donation>)itemsProperty.GetValue(groupObject);
                    if (items != null)
                    {
                        FilteredSum += items.Sum(d => d.NetAmount);
                    }
                }
            }
            StateHasChanged();
        }

        public async Task OnActionComplete(ActionEventArgs<Donation> args)
        {
            if (args.RequestType == Action.Filtering || args.RequestType == Action.Refresh)
            {
                await UpdateFilteredSum();
            }
        }
        private async Task LoadDonationCampaigns()
        {
            var result = await _svcDonationCampaign.GetAllAsync();
            if (result.Success && result.Data != null)
            {
                DonationCampaigns = result.Data.ToList();
                var item = DonationCampaigns.Single(r => r.LocationId == (int)LocationNumber.National);
                if (item != null)
                {
                    DonationCampaigns.Remove(item);
                }
            }
            else
            {
                Log.Error($"Error loading donation campaigns: {result.Message}");
                _toastService.Error("Error loading donation campaigns", "An error occurred while loading donation campaigns. Please try again later.");
                DonationCampaigns = new List<DonationCampaign>();
            }

            bool isNationalAdmin = _svcUser.IsUserNationalAdmin();
            if (!isNationalAdmin)
            {
                int userLocationId = _svcUser.GetUserLocationId();
                DonationCampaigns = DonationCampaigns.Where(o => o.LocationId == userLocationId).ToList();
            }
        }

        private void LoadNotSent()
        {
            NotSent = Donations.Where(d => !d.TaxFormSent)
                .OrderBy(o => o.FullName)
                .ThenBy(o => o.DonationDate).ToList();
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (_svcAuth.UserHasRole(RoleNames.CanManageDonations))
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
            if (locationResult.Success && locationResult.Data != null)
            {
                Locations = locationResult.Data.ToList();
                var item = Locations.Single(r => r.LocationId == (int)LocationNumber.National);
                if (item != null)
                {
                    Locations.Remove(item);
                }
            }
            else
            {
                Log.Error($"Error loading locations: {locationResult.Message}");
                _toastService.Error("Error loading locations", "An error occurred while loading locations. Please try again later.");
                Locations = new List<Location>();
            }

            bool isNationalAdmin = _svcUser.IsUserNationalAdmin();
            if (!isNationalAdmin)
            {
                int userLocationId = _svcUser.GetUserLocationId();
                Locations = Locations.Where(o => o.LocationId == userLocationId).ToList();
            }
        }

        private async Task LoadDonations()
        {
            bool isNationalAdmin = _svcUser.IsUserNationalAdmin();
            if (isNationalAdmin)
            {
                var allResult = await _svcDonation.GetAllAsync();

                if (allResult.Success && allResult.Data != null)
                {
                    Donations = allResult.Data.ToList();
                }
                else
                {
                    Log.Error($"Error loading donations: {allResult.Message}");
                    _toastService.Error("Error loading donations", "An error occurred while loading donations. Please try again later.");
                    Donations = new List<Donation>();
                }
            }
            else
            {
                int userLocationId = _svcUser.GetUserLocationId();
                var contactUsResult = await _svcDonation.GetAllForLocationAsync(userLocationId);
                if (contactUsResult.Success && contactUsResult.Data != null)
                {
                    Donations = contactUsResult.Data.ToList();
                }
                else
                {
                    Log.Error($"Error loading donations for location {userLocationId}: {contactUsResult.Message}");
                    _toastService.Error("Error loading donations", "An error occurred while loading donations. Please try again later.");
                    Donations = new List<Donation>();
                }
            }
        }

        private void SetupAccess()
        {
            if (_svcAuth.UserHasRole(RoleNames.CanManageDonations))
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
            else 
            {
                ToolBar = new List<string> { "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<object> {
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
            string userName = _svcUser.GetUserName();
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
            string userName = _svcUser.GetUserName();
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
                LoadNotSent();
                TaxIsVisible = true;
            }
        }

        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if(args.Item.Text == SendTaxForm)
            {
                LoadNotSent();
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
            Donation.LocationId = _svcAuth.LocationId;
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
            await Grid.Refresh();
        }

        protected async void DataBound()
        {
            if (Donations.Count == 0) RecordText = "No Donation records found";
            if (Grid.TotalItemCount <= Grid.PageSettings.PageSize)  //compare total grid data count with pagesize value 
            {
                NoPaging = true;
            }
            else
            {
                NoPaging = false;
            }

            await UpdateFilteredSum();
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
            var result =  await _svcEmailBuilder.EmailTaxForms(LB_Send.GetDataList().ToList());

            if (result.Success)
            {
                _toastService.Success("Tax Forms Sent", "Tax Forms Sent Successfully!");
                await LoadDonations();
                LoadNotSent();
                Grid.Refresh();
            }
            else
            {
                _toastService.Error("Could not send tax forms", "Unable to send Tax Forms!");
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
        public  SfListBox<string[], Donation> LB_NotSent;
        public  SfListBox<string[], Donation> LB_Send;
        private List<Donation> NotSent = new List<Donation>();
        private List<Donation> Send = new List<Donation>();

        #endregion

    }
}

