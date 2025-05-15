using BedBrigade.Common.Logic;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using System.Globalization;
using BedBrigade.Common.Constants;
using BedBrigade.Client.Services;
using BedBrigade.Data.Data.Seeding;

namespace BedBrigade.Client.Components.Pages
{
    public partial class BlogCard : ComponentBase, IDisposable
    {
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private NavigationManager _nav { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ILanguageService _svcLanguage { get; set; }
        [Inject] private ITranslationDataService _translateLogic { get; set; }
        [Inject] private IContentTranslationDataService _svcContentTranslation { get; set; }
        [Inject] private ILocationState _locationState { get; set; }

        [Parameter]
        public string? LocationRoute { get; set; }
        [Parameter]
        public string? Name { get; set; }

        [Parameter]
        public string? BlogType { get; set; }

        [Parameter]
        public string? Filter { get; set; }

        private Content? ContentItem;
        public string? ErrorMessage { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        private string previousLocation = SeedConstants.SeedNationalName;
        public string? BackUrl { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
            string url = _nav.Uri;

            ServiceResponse<Location>? locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");
            if (locationResponse != null && locationResponse.Success && locationResponse.Data != null)
            {
                LocationId = locationResponse.Data.LocationId;
                LocationName = locationResponse.Data.Name;
                await LoadContent();
            }
            else
            {
                ErrorMessage = $"Location not found: {LocationRoute}";
            }

            if (string.IsNullOrEmpty(Filter))
            {
                BackUrl = $"/{LocationRoute}/Blog/{BlogType}";
            }
            else
            {
                BackUrl = $"/{LocationRoute}/Blog/{BlogType}/{Filter}";
            }
            
            _svcLanguage.LanguageChanged += OnLanguageChanged;
        }

        protected override void OnParametersSet()
        {
            _locationState.Location = LocationRoute;
        }

        public void Dispose()
        {
            _svcLanguage.LanguageChanged -= OnLanguageChanged;
        }

        private async Task LoadContent()
        {
            if (ContentItem == null || _svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
            {
                await LoadDefaultContent();
            }

            if (_svcLanguage.CurrentCulture.Name != Defaults.DefaultLanguage)
            {
                await LoadByLanguage();
            }
        }

        private async Task LoadDefaultContent()
        {
            var response = await _svcContent.GetAsync(Name, LocationId.Value);
            if (response.Success && response.Data is not null)
            {
                ContentItem = response.Data;
            }
            else
            {
                ErrorMessage = $"{Pluralization.MakeSingular(BlogType)} not found: {Name} ";
            }
        }

        private async Task OnLanguageChanged(CultureInfo arg)
        {
            await LoadContent();
            StateHasChanged();
        }

        private async Task LoadByLanguage()
        {
            var contentResult = await _svcContentTranslation.GetAsync(Name, LocationId.Value, _svcLanguage.CurrentCulture.Name);

            if (contentResult.Success && contentResult.Data != null)
            {
                ContentItem.ContentHtml = contentResult.Data.ContentHtml;
                ContentItem.Title = await _translateLogic.GetTranslation(contentResult.Data.Title, _svcLanguage.CurrentCulture.Name);
            }
            else
            {
                ErrorMessage = $"{Pluralization.MakeSingular(BlogType)} translation for {_svcLanguage.CurrentCulture} not found: {Name} ";
            }
        }
    }
}