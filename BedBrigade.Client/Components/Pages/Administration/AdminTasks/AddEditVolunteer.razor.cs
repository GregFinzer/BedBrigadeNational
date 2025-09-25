using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.Inputs;

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
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ISpokenLanguageDataService _svcSpokenLanguage { get; set; }
        public string ErrorMessage { get; set; }
        public Volunteer? Model { get; set; }
        private const string ErrorTitle = "Error";
        protected List<Location>? Locations { get; private set; }
        public bool CanSetLocation = false;
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "5" },
        };
        public required SfMaskedTextBox phoneTextBox;
        private string[] SelectedLanguages { get; set; } = [];
        private List<SpokenLanguage> SpokenLanguages { get; set; } = [];
        private List<EnumNameValue<CanYouTranslate>> TranslationOptions { get; set; }
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Log.Information($"{_svcAuth.UserName} went to the Add/Edit Volunteer Page");

                bool isNationalAdmin = _svcUser.IsUserNationalAdmin();
                if (isNationalAdmin)
                {
                    CanSetLocation = true;
                }

                //This GetAllAsync should always have less than 1000 records
                SpokenLanguages = (await _svcSpokenLanguage.GetAllAsync()).Data;
                TranslationOptions = EnumHelper.GetEnumNameValues<CanYouTranslate>();
                await LoadLocations();
                await LoadVolunteer();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing AddEditVolunteer component");
                _toastService.Error(ErrorTitle, "An error occurred while loading the volunteer data.");
            }
        }

        private async Task LoadLocations()
        {
            //This GetAllAsync should always have less than 1000 records
            var result = await _svcLocation!.GetAllAsync();

            if (result.Success) // 
            {
                Locations = result.Data;
            }
            else
            {
                Log.Error($"AddEditVolunteer, Error loading locations: {result.Message}");
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
                    SelectedLanguages = Model!.OtherLanguagesSpoken.Replace(" ", string.Empty).Split(',');
                }
                else
                {
                    Log.Error($"AddEditVolunteer, Error loading volunteer with ID {VolunteerId}: {result.Message}");
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
            Model.OtherLanguagesSpoken = string.Join(", ", SelectedLanguages);

            if (VolunteerId != null)
            {
                var updateResult = await _volunteerDataService.UpdateAsync(Model!);
                if (updateResult.Success)
                {
                    _toastService.Success("Success", "Volunteer updated successfully");
                    return true;
                }

                Log.Error("AddEditVolunteer, Could not update volunteer " + updateResult.Message);
                _toastService.Error(ErrorTitle, updateResult.Message);
                return false;
            }

            var existingVolunteerResult = await _volunteerDataService.GetByPhone(Model.Phone);
            if (existingVolunteerResult.Success)
            {
                ErrorMessage = "A volunteer with this phone already exists.";
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

        public async Task HandlePhoneMaskFocus()
        {
            await JS.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
        }
    }
}
