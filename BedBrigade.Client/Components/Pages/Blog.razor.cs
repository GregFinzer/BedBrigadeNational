using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Location = BedBrigade.Common.Models.Location;

namespace BedBrigade.Client.Components.Pages
{
    public partial class Blog : ComponentBase
    {
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private NavigationManager _nav { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }

        [Parameter]
        public string LocationRoute { get; set; } = default!;

        public int? LocationId { get; set; }

        public string RotatorTitle { get; set; }

        public string BlogType { get; set; }

        private List<BlogItem>? BlogItems;

        public string LocationName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
            string uri = _nav.Uri;
            uri = uri.TrimEnd('/');
            BlogType = StringUtil.GetLastWord(uri, "/");

            BlogItems = new List<BlogItem>();
            ServiceResponse<Location>? locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");
            if (locationResponse != null && locationResponse.Success && locationResponse.Data != null)
            {
                LocationId = locationResponse.Data.LocationId;
                LocationName = locationResponse.Data.Name;
                RotatorTitle = $"{locationResponse.Data.Name} {BlogType}";

                ContentType contentType = Enum.Parse<ContentType>(BlogType, true);

                var contentResponse = await _svcContent.GetBlogItems(LocationId.Value, contentType);
                if (contentResponse.Success && contentResponse.Data is not null)
                {
                    BlogItems = contentResponse.Data;
                }
            }
        }


    }
}