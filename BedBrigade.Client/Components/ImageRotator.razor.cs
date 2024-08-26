using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components
{
    public partial class ImageRotator : ComponentBase
    {
        [Parameter] public string mylocation { get; set; }
        [Parameter] public string myId { get; set; }
        [Parameter] public string myPath { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ILoadImagesService _svcLoadImages { get; set; }
        private bool isLoading = true;
        private bool imagesFound = false;
        private string nationalPath = string.Empty;
        private string locationPath = string.Empty;
        private string imagePath = string.Empty;
        
        protected override async Task OnInitializedAsync()
        {
            var location = await _svcLocation.GetByIdAsync(Convert.ToInt32(mylocation));
            var locationImageList = _svcLoadImages.GetImagesForArea($"{location.Data.Route}/{myPath}",myId);

            if (locationImageList.Count > 0)
            {
                imagesFound = true;
                imagePath = _svcLoadImages.GetRotatedImage(locationImageList);
                isLoading = false;
                return;
            }
            var nationalImageList = _svcLoadImages.GetImagesForArea($"{Defaults.NationalRoute}/{myPath}", myId);

            if (nationalImageList.Count > 0)
            {
                imagesFound = true;
                imagePath = _svcLoadImages.GetRotatedImage(nationalImageList);
                isLoading = false;
                return;
            }

            locationPath = _svcLoadImages.GetDirectoryForPathAndArea("{location.Data.Route}/{myPath}", myId);
            nationalPath = _svcLoadImages.GetDirectoryForPathAndArea("{Constants.NationalRoute}/{myPath}", myId);
            imagesFound = false;
            isLoading = false;
        }
    }
}
