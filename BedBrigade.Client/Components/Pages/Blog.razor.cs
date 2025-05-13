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
        public bool Older { get; set; }
        public string Uri { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);

            SetParameters();

            BlogItems = new List<BlogItem>();
            ServiceResponse<Location>? locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");
            if (locationResponse != null && locationResponse.Success && locationResponse.Data != null)
            {
                LocationId = locationResponse.Data.LocationId;
                LocationName = locationResponse.Data.Name;
                RotatorTitle = $"{locationResponse.Data.Name} {BlogType}";

                ContentType contentType = Enum.Parse<ContentType>(BlogType, true);

                if (Older)
                {
                    var contentResponse = await _svcContent.GetOlderBlogItems(LocationId.Value, contentType);
                    if (contentResponse.Success && contentResponse.Data is not null)
                    {
                        BlogItems = contentResponse.Data;
                    }
                }
                else
                {
                    var contentResponse = await _svcContent.GetTopBlogItems(LocationId.Value, contentType);
                    if (contentResponse.Success && contentResponse.Data is not null)
                    {
                        BlogItems = contentResponse.Data;
                    }
                }
            }
        }
        void GoToOlder()
        {
            _nav.NavigateTo($"{Uri}/older", forceLoad: true);
        }

        void GoToNewer()
        {
            string newUrl = StringUtil.TakeOffEnd(Uri, "older");
            _nav.NavigateTo(newUrl, forceLoad: true);
        }

        private void SetParameters()
        {
            Uri = _nav.Uri;
            Uri = Uri.TrimEnd('/');
            Older = Uri.EndsWith("/older", StringComparison.InvariantCultureIgnoreCase);

            if (Older)
            {
                BlogType = StringUtil.GetNextToLastWord(Uri, "/");
            }
            else
            {
                BlogType = StringUtil.GetLastWord(Uri, "/");
            }
        }
    }
}