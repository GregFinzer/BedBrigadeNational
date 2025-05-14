using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Location = BedBrigade.Common.Models.Location;
using System.Globalization;
using BedBrigade.Common.Constants;


namespace BedBrigade.Client.Components.Pages
{
    public partial class Blog : ComponentBase, IDisposable
    {
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private NavigationManager _nav { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ILanguageService _svcLanguage { get; set; }
        [Inject] private ITranslationDataService _translateLogic { get; set; }
        [Inject] private IContentTranslationDataService _svcContentTranslation { get; set; }

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
                RotatorTitle = $"{locationResponse.Data.Name} {StringUtil.ProperCase(BlogType)}";

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

            _svcLanguage.LanguageChanged += OnLanguageChanged;
        }

        private async Task OnLanguageChanged(CultureInfo arg)
        {
            await TranslatePageTitle();
            await TranslateTitles();
            await TranslateDescriptions();
            StateHasChanged();
        }

        public void Dispose()
        {
            _svcLanguage.LanguageChanged -= OnLanguageChanged;
        }

        private async Task TranslatePageTitle()
        {
            RotatorTitle = $"{LocationName} {StringUtil.ProperCase(BlogType)}";

            if (_svcLanguage.CurrentCulture.Name != Defaults.DefaultLanguage)
            {
                RotatorTitle = await _translateLogic.GetTranslation(RotatorTitle, _svcLanguage.CurrentCulture.Name);
            }
        }

        private async Task TranslateDescriptions()
        {
            if (_svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
            {
                foreach (var blogItem in BlogItems)
                {
                    blogItem.Description = StringUtil.TruncateTextToLastWord(WebHelper.StripHTML(blogItem.ContentHtml),
                        Defaults.TruncationLength);
                }

                return;
            }

            foreach (var blogItem in BlogItems)
            {
                var contentResult = await _svcContentTranslation.GetAsync(blogItem.Name, LocationId.Value, _svcLanguage.CurrentCulture.Name);

                if (contentResult.Success && contentResult.Data != null)
                {
                    blogItem.Description = StringUtil.TruncateTextToLastWord(WebHelper.StripHTML(contentResult.Data.ContentHtml),
                        Defaults.TruncationLength);
                }
            }
        }

        private async Task TranslateTitles()
        {
            if (_svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
            {
                foreach (var blogItem in BlogItems)
                {
                    blogItem.TitleTranslated = blogItem.Title;
                }

                return;
            }

            foreach (var blogItem in BlogItems)
            {
                blogItem.TitleTranslated = await _translateLogic.GetTranslation(blogItem.Title, _svcLanguage.CurrentCulture.Name);
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