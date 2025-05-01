using BedBrigade.Common.Logic;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;

namespace BedBrigade.Client.Components.Pages
{
    public partial class BlogCard : ComponentBase
    {
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private NavigationManager _nav { get; set; }

        [Parameter]
        public string? LocationRoute { get; set; }
        [Parameter]
        public string? Name { get; set; }

        public string? BlogType { get; set; }

        private Content? ContentItem;


        protected override async Task OnInitializedAsync()
        {
            string url = _nav.Uri;
            BlogType = StringUtil.GetNextToLastWord(url, "/");

            int locationId;

            ServiceResponse<Location>? locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");
            if (locationResponse != null && locationResponse.Success && locationResponse.Data != null)
            {
                locationId = locationResponse.Data.LocationId;

                var response = await _svcContent.GetAsync(Name, locationId);
                if (response.Success && response.Data is not null)
                {
                    ContentItem = response.Data;
                }
                else
                {
                    // TODO: handle not-found or error (e.g. show a message or redirect)
                }
            }
        }
    }
}