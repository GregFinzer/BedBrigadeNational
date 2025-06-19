using System.Globalization;
using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace BedBrigade.Client.Components
{
    public partial class Footer : ComponentBase, IDisposable
    {
        // Client
        private string footerContent = string.Empty;
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ILocationState _locationState { get; set; }
        [Inject] private IContentTranslationDataService _svcContentTranslation { get; set; }

        [Inject] private ILanguageService _svcLanguage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadContent();
            _locationState.OnChange += OnLocationChanged;
            _svcLanguage.LanguageChanged += OnLanguageChanged;
        }

        private async Task OnLanguageChanged(CultureInfo arg)
        {
            await LoadContent();
            StateHasChanged();
        }

        public void Dispose()
        {
            _locationState.OnChange -= OnLocationChanged;
            _svcLanguage.LanguageChanged -= OnLanguageChanged;
        }

        private async Task LoadContent()
        {
            try
            {
                string locationName = _locationState.Location ?? SeedConstants.SeedNationalName;
                var locationResult = await _svcLocation.GetLocationByRouteAsync($"/{locationName.ToLower()}");

                if (!locationResult.Success && locationResult.Data != null)
                {
                    Log.Logger.Error($"Error loading Footer location: {locationResult.Message}");
                }
                else
                {
                    if (_svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
                    {
                        await LoadDefaultContent(locationResult, locationName);
                    }
                    else
                    {
                        await LoadContentByLanguage(locationResult, locationName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error loading Footer content");
            }
        }

        private async Task LoadContentByLanguage(ServiceResponse<Location> locationResult, string locationName)
        {
            var contentResult = await _svcContentTranslation.GetAsync("Footer", locationResult.Data.LocationId, _svcLanguage.CurrentCulture.Name);

            if (contentResult.Success)
            {
                footerContent = contentResult.Data.ContentHtml;
            }
            else
            {
                await LoadDefaultContent(locationResult, locationName);
            }
        }

        private async Task LoadDefaultContent(ServiceResponse<Location> locationResult, string locationName)
        {
            var contentResult = await _svcContent.GetAsync("Footer", locationResult.Data.LocationId);

            if (contentResult.Success && contentResult.Data != null)
            {
                footerContent = contentResult.Data.ContentHtml;
            }
            else
            {
                Log.Logger.Error($"Error loading Footer for LocationId {locationResult.Data.LocationId}: {contentResult.Message}");
            }
        }

        private async Task OnLocationChanged()
        {
            await LoadContent();
            StateHasChanged();
        }
    }
}
