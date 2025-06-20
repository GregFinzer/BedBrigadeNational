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
        [Parameter] public string mylocation { get; set; }
        [Parameter] public string myId { get; set; }
        [Parameter] public string myPath { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ILoadImagesService _svcLoadImages { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        private bool isLoading = true;
        private bool imagesFound = false;
        private string nationalPath = string.Empty;
        private string locationPath = string.Empty;
        private string imagePath = string.Empty;
        
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await LoadImagesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading images in ImageRotator component");
                isLoading = false;
                imagesFound = false;
            }
        }

        private async Task LoadImagesAsync()
        {
            _lc.InitLocalizedComponent(this);
            ServiceResponse<Location> locationResult = await _svcLocation.GetByIdAsync(Convert.ToInt32(mylocation));

            if (!locationResult.Success || locationResult.Data == null)
            {
                Log.Error("Failed to load location with ID {LocationId}: {Message}", mylocation, locationResult.Message);
                isLoading = false;
                imagesFound = false;
                return;
            }

            List<string> locationImageList = _svcLoadImages.GetImagesForArea($"{locationResult.Data.Route}/{myPath}", myId);

            if (locationImageList.Count > 0)
            {
                imagesFound = true;
                imagePath = _svcLoadImages.GetRotatedImage(locationImageList);
                isLoading = false;
                return;
            }
            List<string> nationalImageList = _svcLoadImages.GetImagesForArea($"{Defaults.NationalRoute}/{myPath}", myId);

            if (nationalImageList.Count > 0)
            {
                imagesFound = true;
                imagePath = _svcLoadImages.GetRotatedImage(nationalImageList);
                isLoading = false;
                return;
            }

            locationPath = _svcLoadImages.GetDirectoryForPathAndArea($"{locationResult.Data.Route}/{myPath}", myId);
            nationalPath = _svcLoadImages.GetDirectoryForPathAndArea($"{Defaults.NationalRoute}/{myPath}", myId);
            imagesFound = false;
            isLoading = false;
        }
    }
}
