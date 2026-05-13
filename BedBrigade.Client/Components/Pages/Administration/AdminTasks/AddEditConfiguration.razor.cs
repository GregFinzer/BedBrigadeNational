using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks
{
    public partial class AddEditConfiguration : ComponentBase
    {
        private const string EncryptedValueCssClass = "encrypted-value";
        private const string EyeIconCssClass = "fas fa-eye";
        private const string EyeSlashIconCssClass = "fas fa-eye-slash";
        private const string ManageConfigurationPageUrl = "/administration/manage/Configuration";

        [Parameter] public int LocationId { get; set; }
        [Parameter] public int? ConfigurationId { get; set; }

        [Inject] private IConfigurationDataService SvcConfiguration { get; set; } = default!;
        [Inject] private ILocationDataService SvcLocation { get; set; } = default!;
        [Inject] private IAuthService SvcAuth { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        protected Configuration Model { get; set; } = new();
        protected List<Location> Locations { get; private set; } = new();
        protected List<ConfigSectionEnumItem> ConfigSectionEnumItems { get; private set; } = new();
        protected bool IsLoading { get; private set; } = true;
        protected bool IsDecryptedValueVisible { get; private set; }
        protected string ErrorMessage { get; private set; } = string.Empty;
        protected bool IsNew => !ConfigurationId.HasValue || ConfigurationId.Value == 0;
        protected string HeaderText => IsNew ? "Add Configuration" : "Update Configuration";
        protected string ButtonText => IsNew ? "Add Configuration" : "Update Configuration";
        protected bool CanToggleValueVisibility => Model.Encrypted;
        protected string ValueVisibilityButtonTitle => IsDecryptedValueVisible ? "Hide decrypted value" : "Show decrypted value";
        protected string ValueVisibilityIconCssClass => IsDecryptedValueVisible ? EyeSlashIconCssClass : EyeIconCssClass;

        protected string ConfigurationValueEditor
        {
            get => Model.DecryptedValue;
            set
            {
                Model.DecryptedValue = value;
                Model.ConfigurationValue = value;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Log.Information("{UserName} opened the Add/Edit Configuration page", SvcAuth.UserName);
                ConfigSectionEnumItems = EnumHelper.GetConfigSectionItems();
                await LoadLocationsAsync();
                await LoadModelAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing AddEditConfiguration");
                ErrorMessage = "An error occurred while loading the configuration editor.";
                ToastService.Error("Configuration Editor", ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadLocationsAsync()
        {
            var result = await SvcLocation.GetActiveLocations();
            if (result.Success && result.Data != null)
            {
                Locations = result.Data.OrderBy(location => location.Name).ToList();
                return;
            }

            ErrorMessage = result.Message;
            Log.Error("Unable to load locations for configuration editor: {Message}", ErrorMessage);
            ToastService.Error("Load Locations", ErrorMessage);
        }

        private async Task LoadModelAsync()
        {
            if (IsNew)
            {
                Model = BuildNewConfiguration();
                return;
            }

            var result = await SvcConfiguration.GetByIdAsync(ConfigurationId!.Value);
            if (result.Success && result.Data != null)
            {
                Model = result.Data;
                return;
            }

            ErrorMessage = result.Message;
            Log.Error("Unable to load configuration {ConfigurationId}: {Message}", ConfigurationId, ErrorMessage);
            ToastService.Error("Load Configuration", ErrorMessage);
            Model = BuildNewConfiguration();
        }

        private Configuration BuildNewConfiguration()
        {
            return new Configuration
            {
                LocationId = LocationId == 0 ? Defaults.NationalLocationId : LocationId,
                Section = ConfigSection.System,
                ConfigurationKey = string.Empty,
                ConfigurationValue = string.Empty,
                Encrypted = false
            };
        }

        protected async Task HandleValidSubmitAsync()
        {
            ErrorMessage = string.Empty;
            Model.ConfigurationValue = Model.DecryptedValue;

            try
            {
                if (IsNew)
                {
                    await CreateConfigurationAsync();
                    return;
                }

                await UpdateConfigurationAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving configuration {ConfigurationKey}", Model.ConfigurationKey);
                ErrorMessage = "An error occurred while saving the configuration.";
                ToastService.Error("Save Configuration", ErrorMessage);
            }
        }

        private async Task CreateConfigurationAsync()
        {
            var existing = await SvcConfiguration.GetAllForLocationAsync(Model.LocationId);
            if (existing.Success
                && existing.Data != null
                && existing.Data.Any(configuration => configuration.ConfigurationKey.Equals(Model.ConfigurationKey, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "Configuration Key already exists for this location!";
                ToastService.Error("Add Configuration Error", ErrorMessage);
                return;
            }

            var createResult = await SvcConfiguration.CreateAsync(Model);
            if (!createResult.Success)
            {
                ErrorMessage = createResult.Message;
                ToastService.Error("Add Configuration Error", ErrorMessage);
                return;
            }

            ToastService.Success("Add Configuration Success", "Configuration Added Successfully!");
            NavigationManager.NavigateTo(ManageConfigurationPageUrl);
        }

        private async Task UpdateConfigurationAsync()
        {
            var updateResult = await SvcConfiguration.UpdateAsync(Model);
            if (!updateResult.Success)
            {
                ErrorMessage = updateResult.Message;
                ToastService.Error("Update Configuration Error", ErrorMessage);
                return;
            }

            ToastService.Success("Update Configuration Success", "Configuration Updated Successfully!");
            NavigationManager.NavigateTo(ManageConfigurationPageUrl);
        }

        protected void ToggleDecryptedValueVisibility()
        {
            if (!CanToggleValueVisibility)
            {
                IsDecryptedValueVisible = false;
                return;
            }

            IsDecryptedValueVisible = !IsDecryptedValueVisible;
        }

        protected string GetConfigurationValueCssClass()
        {
            return Model.Encrypted && !IsDecryptedValueVisible
                ? EncryptedValueCssClass
                : string.Empty;
        }

        protected void HandleCancel()
        {
            NavigationManager.NavigateTo(ManageConfigurationPageUrl);
        }
    }
}



