using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace BedBrigade.Client.Components
{
    public partial class ImageRotator : ComponentBase
    {
        [Parameter] public string Location { get; set; } = string.Empty;
        [Parameter] public string ImageId { get; set; } = string.Empty;
        [Parameter] public string ImagePath { get; set; } = string.Empty;
        [Inject] private ILocationDataService _svcLocation { get; set; } = default!;
        [Inject] private ILoadImagesService _svcLoadImages { get; set; } = default!;
        [Inject] private ILanguageContainerService _lc { get; set; } = default!;
        private bool _isLoading = true;
        private bool _imagesFound;
        private string _nationalPath = string.Empty;
        private string _locationPath = string.Empty;
        private string _imagePath = string.Empty;
        private string _previousLocation = string.Empty;
        private string _previousId = string.Empty;
        private string _previousPath = string.Empty;
        
        protected override async Task OnParametersSetAsync()
        {
            try
            {
                if (!HaveParametersChanged())
                {
                    return;
                }

                _previousLocation = Location;
                _previousId = ImageId;
                _previousPath = ImagePath;

                ResetImageState();
                await LoadImagesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading images in ImageRotator component");
                _isLoading = false;
                _imagesFound = false;
            }
        }

        private bool HaveParametersChanged()
        {
            return !string.Equals(Location, _previousLocation, StringComparison.Ordinal)
                   || !string.Equals(ImageId, _previousId, StringComparison.Ordinal)
                   || !string.Equals(ImagePath, _previousPath, StringComparison.Ordinal);
        }

        private void ResetImageState()
        {
            _isLoading = true;
            _imagesFound = false;
            _nationalPath = string.Empty;
            _locationPath = string.Empty;
            _imagePath = string.Empty;
        }

        private async Task LoadImagesAsync()
        {
            _lc.InitLocalizedComponent(this);

            if (!int.TryParse(Location, out int locationId))
            {
                Log.Error("Invalid location ID '{LocationId}' for {ComponentName}", Location, nameof(ImageRotator));
                _isLoading = false;
                _imagesFound = false;
                return;
            }

            ServiceResponse<Location> locationResult = await _svcLocation.GetByIdAsync(locationId);

            if (!locationResult.Success || locationResult.Data == null)
            {
                Log.Error("Failed to load location with ID {LocationId}: {Message}", Location, locationResult.Message);
                _isLoading = false;
                _imagesFound = false;
                return;
            }

            List<string> locationImageList = _svcLoadImages.GetImagesForArea($"{locationResult.Data.Route}/{ImagePath}", ImageId);

            if (locationImageList.Count > 0)
            {
                _imagesFound = true;
                _imagePath = _svcLoadImages.GetRotatedImage(locationImageList);
                _isLoading = false;
                return;
            }

            List<string> nationalImageList = _svcLoadImages.GetImagesForArea($"{Defaults.NationalRoute}/{ImagePath}", ImageId);

            if (nationalImageList.Count > 0)
            {
                _imagesFound = true;
                _imagePath = _svcLoadImages.GetRotatedImage(nationalImageList);
                _isLoading = false;
                return;
            }

            _locationPath = _svcLoadImages.GetDirectoryForPathAndArea($"{locationResult.Data.Route}/{ImagePath}", ImageId);
            _nationalPath = _svcLoadImages.GetDirectoryForPathAndArea($"{Defaults.NationalRoute}/{ImagePath}", ImageId);
            _imagesFound = false;
            _isLoading = false;
        }
    }
}
