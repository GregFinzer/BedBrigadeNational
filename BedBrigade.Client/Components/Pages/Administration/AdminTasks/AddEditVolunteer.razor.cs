using System.Drawing;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Serilog;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using BedBrigade.Client.Services;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks
{
    public partial class AddEditVolunteer : ComponentBase
    {
        [Parameter] public int? VolunteerId { get; set; }
        [Parameter] public int? LocationId { get; set; }

        [Inject] private NavigationManager? _navigationManager { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private IVolunteerDataService _volunteerDataService { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        private ClaimsPrincipal? Identity { get; set; }
        public string ErrorMessage { get; set; }
        public Volunteer? Model { get; set; }
        private const string ErrorTitle = "Error";
        protected List<Location>? Locations { get; private set; }
        public bool CanSetLocation = false;
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "5" },
        };
        

        protected override async Task OnInitializedAsync()
        {
            Identity = _svcAuth.CurrentUser;

            var userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Add/Edit Volunteer Page");

            if (Identity.IsInRole(RoleNames.NationalAdmin))
            {
                CanSetLocation = true;
            }

            await LoadLocations();
            await LoadVolunteer();
        }

        private async Task LoadLocations()
        {
            var result = await _svcLocation!.GetAllAsync();

            if (result.Success) // 
            {
                Locations = result.Data;
            }
            else
            {
                Log.Error($"Error loading locations: {result.Message}");
                _toastService.Error(ErrorTitle, result.Message);
            }
        } 

        private async Task LoadVolunteer()
        {
            if (VolunteerId != null)
            {
                var result = await _volunteerDataService.GetByIdAsync(VolunteerId.Value);

                if (result.Success)
                {
                    Model = result.Data;
                }
                else
                {
                    Log.Error($"Error loading volunteer with ID {VolunteerId}: {result.Message}");
                    _toastService.Error(ErrorTitle, result.Message);
                }
            }
            else
            {
                Model = new Volunteer();
                Model.LocationId = LocationId.Value;
            }
        }

        private async Task HandleValidSubmit()
        {
            if (IsValid() && await SaveVolunteer())
            {
                _navigationManager?.NavigateTo("/administration/manage/volunteers");
            }
        }

        private bool IsValid()
        {
            ErrorMessage = string.Empty;
            bool isPhoneValid = Validation.IsValidPhoneNumber(Model.Phone);

            if (!isPhoneValid)
            {
                ErrorMessage = "Phone numbers must be 10 digits with a valid area code and prefix.";
                return false;
            }

            var emailResult = Validation.IsValidEmail(Model.Email);
            if (!emailResult.IsValid)
            {
                ErrorMessage = emailResult.UserMessage;
                return false;
            }

            return true;
        }

        private async Task<bool> SaveVolunteer()
        {
            if (VolunteerId != null)
            {
                var updateResult = await _volunteerDataService.UpdateAsync(Model!);
                if (updateResult.Success)
                {
                    _toastService.Success("Success", "Volunteer updated successfully");
                    return true;
                }

                Log.Error("Could not update volunteer " + updateResult.Message);
                _toastService.Error(ErrorTitle, updateResult.Message);
                return false;
            }

            var existingVolunteerResult = await _volunteerDataService.GetByEmail(Model.Email);
            if (existingVolunteerResult.Success)
            {
                ErrorMessage = "A volunteer with this email already exists.";
                return false;
            }

            var result = await _volunteerDataService.CreateAsync(Model!);
            if (result.Success)
            {
                _toastService.Success("Success", "Volunteer created successfully");
                return true;
            }

            _toastService.Error(ErrorTitle, result.Message);
            return false;
        }

        private void HandleCancel()
        {
            _navigationManager.NavigateTo("/administration/manage/volunteers");
        }
    }
}
