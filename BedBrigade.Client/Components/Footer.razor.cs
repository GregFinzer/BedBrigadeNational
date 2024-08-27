using BedBrigade.Client.Services;
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
        [Inject] NavigationManager _nm { get; set; }
        [Inject] private ILocationState _locationState { get; set; }
        private string PreviousLocation { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadContent();
            _locationState.OnChange += OnLocationChanged;
        }

        public void Dispose()
        {
            _locationState.OnChange -= OnLocationChanged;
        }

        private async Task LoadContent()
        {
            string locationName = _locationState.Location ?? SeedConstants.SeedNationalName;
            var locationResult = await _svcLocation.GetLocationByRouteAsync($"/{locationName.ToLower()}");

            if (!locationResult.Success)
            {
                Log.Logger.Error($"Error loading Footer location: {locationResult.Message}");
            }
            else
            {
                var contentResult = await _svcContent.GetAsync("Footer", locationResult.Data.LocationId);

                if (contentResult.Success)
                {
                    footerContent = contentResult.Data.ContentHtml;
                    PreviousLocation = locationName;
                }
                else
                {
                    Log.Logger.Error($"Error loading Footer for LocationId {locationResult.Data.LocationId}: {contentResult.Message}");
                }
            }
        }

        private async Task OnLocationChanged()
        {
            await LoadContent();
            StateHasChanged();
        }
    }
}
