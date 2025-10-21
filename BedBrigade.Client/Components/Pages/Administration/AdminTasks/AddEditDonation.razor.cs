using BedBrigade.Client.Components.Pages.Administration.Manage;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Migrations;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.Inputs;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks
{
    public partial class AddEditDonation : ComponentBase
    {
        [Parameter] public int LocationId { get; set; }
        [Parameter] public int? DonationId { get; set; }

        [Inject] private IDonationCampaignDataService? _svcDonationCampaign { get; set; }
        [Inject] private IDonationDataService? _svcDonation { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private ToastService? _toastService { get; set; }
        [Inject] private NavigationManager? _nav { get; set; }
        [Inject] private ILanguageContainerService? _lc { get; set; }
        protected Donation Model { get; set; } = new Donation();
        protected List<Location>? Locations { get; set; }
        protected List<BedBrigade.Common.Models.DonationCampaign>? DonationCampaigns { get; set; }
        public required SfMaskedTextBox phoneTextBox;

        protected bool IsNew => !DonationId.HasValue || DonationId == 0;
        private string ReturnURL = "/administration/manage/donations";
        private const string ErrorMessage = "Error";

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                await LoadLocations();
                await LoadModel();
                await LoadDonationCampaigns();
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "AddEditDonation.OnInitializedAsync");
                _toastService.Error(ErrorMessage, "An error occurred while initializing the Donation editor.");
            }
        }

        private async Task LoadLocations()
        {
            var result = await _svcLocation.GetActiveLocations();
            if (result.Success)
            {
                Locations = result.Data.ToList();
            }
        }

        private async Task LoadModel()
        {
            if (DonationId.HasValue && DonationId.Value > 0)
            {
                var result = await _svcDonation.GetByIdAsync(DonationId.Value);
                if (result.Success && result.Data != null)
                {
                    Model = result.Data;
                }
                else
                {
                    Log.Error($"AddEditDonation, Error loading Donation {DonationId}: {result.Message}");
                    _toastService.Error(ErrorMessage, result.Message);
                    Model = new Donation { LocationId = LocationId };
                }
            }
            else
            {
                Model = new Donation();
                Model.LocationId = LocationId;
            }
        }

        private async Task LoadDonationCampaigns()
        {
            //This GetAllAsync should always have less than 1000 records
            var result = await _svcDonationCampaign.GetAllAsync();
            if (result.Success && result.Data != null)
            {
                DonationCampaigns = result.Data.ToList();
                var item = DonationCampaigns.Single(r => r.LocationId == Defaults.NationalLocationId);
                if (item != null)
                {
                    DonationCampaigns.Remove(item);
                }
            }
            else
            {
                Log.Error($"Error loading donation campaigns: {result.Message}");
                _toastService.Error("Error loading donation campaigns", "An error occurred while loading donation campaigns. Please try again later.");
                DonationCampaigns = new List<BedBrigade.Common.Models.DonationCampaign>();
            }

            bool isNationalAdmin = _svcUser.IsUserNationalAdmin();
            if (!isNationalAdmin)
            {
                int userLocationId = _svcUser.GetUserLocationId();
                DonationCampaigns = DonationCampaigns.Where(o => o.LocationId == userLocationId).ToList();
            }
        }

        private async Task HandleValidSubmit()
        {
            if (Model.DonationId != 0)
            {
                var updateResult = await _svcDonation.UpdateAsync(Model);
                if (updateResult.Success)
                {
                   
                    _toastService.Success("Donation Updated", "Donation Updated Successfully!");
                    
                }
                else
                {
                    _toastService.Error(ErrorMessage, updateResult.Message);
                    return;
                }
            }
            else
            {
                var result = await _svcDonation.CreateAsync(Model);
                if (result.Success)
                {
                   
                    Model = result.Data;
                     _toastService.Success("Donation Added", "Donation Added Successfully!");
                  
                }
                else
                {
                    _toastService.Error(ErrorMessage, result.Message);
                    return;
                }
            }

            
            _nav.NavigateTo(ReturnURL);
        }

        private void HandleCancel()
        {
            if (_nav != null)
            {                
                _nav.NavigateTo(ReturnURL);
            }                                

        } // Cancel Edit
    }
}