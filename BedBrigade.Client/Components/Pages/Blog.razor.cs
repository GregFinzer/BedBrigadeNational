using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Location = BedBrigade.Common.Models.Location;
using System.Globalization;
using BedBrigade.Common.Constants;
using BedBrigade.Client.Services;
using Serilog;

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
        [Inject] private ILocationState _locationState { get; set; }
        
        private string? previousLocation;
        private string? previousBlogType;

        [Parameter]
        public string LocationRoute { get; set; } = default!;


        public int? LocationId { get; set; }

        public string RotatorTitle { get; set; }

        [Parameter]
        public string BlogType { get; set; }

        [Parameter]
        public string? Filter { get; set; }

        private List<BlogItem>? BlogItems;

        public string LocationName { get; set; }
        public bool Older { get; set; }
        public string Uri { get; set; }
        public string? ErrorMessage { get; set; }
        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);

                SetParameters();

                await LoadData();
                await PerformTranslations();
                _svcLanguage.LanguageChanged += OnLanguageChanged;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while loading the blog: {ex.Message}";
                Log.Error(ex, "Blog OnInitializedAsync");
            }
        }

        private async Task LoadData()
        {
            BlogItems = new List<BlogItem>();
            ServiceResponse<Location>? locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");
            if (locationResponse.Success && locationResponse.Data != null)
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
        }

        protected override async Task OnParametersSetAsync()
        {
            _locationState.Location = LocationRoute;

            //Set to current values first time through
            if (String.IsNullOrEmpty(previousLocation) || String.IsNullOrEmpty(previousBlogType))
            {
                previousLocation = LocationRoute;
                previousBlogType = BlogType;
                return;
            }   

            bool anythingChanged = LocationRoute != previousLocation || BlogType != previousBlogType;

            if (anythingChanged)
            {
                previousLocation = LocationRoute;
                previousBlogType = BlogType;

                await LoadData();
                await PerformTranslations();
                StateHasChanged();
            }
        }

        private async Task OnLanguageChanged(CultureInfo arg)
        {
            await PerformTranslations();
            StateHasChanged();
        }

        public void Dispose()
        {
            _svcLanguage.LanguageChanged -= OnLanguageChanged;
        }

        private async Task PerformTranslations()
        {
            await TranslatePageTitle();
            await TranslateTitles();
            await TranslateDescriptions();
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
            _nav.NavigateTo($"/{LocationRoute}/Blog/{BlogType}/older", forceLoad: true);
        }

        void GoToNewer()
        {
            _nav.NavigateTo($"/{LocationRoute}/Blog/{BlogType}", forceLoad: true);
        }

        private void SetParameters()
        {
            Uri = _nav.Uri;
            Uri = Uri.TrimEnd('/');
            Older = (Filter ?? string.Empty).ToLower() == "older";
        }
    }
}