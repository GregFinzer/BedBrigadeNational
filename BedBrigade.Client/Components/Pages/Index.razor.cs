using BedBrigade.Client.Services;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Services;
using BedBrigade.Common.Logic;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using System;
using System.Data.Entity.Core.Mapping;
using System.Diagnostics;
using System.Globalization;
using BedBrigade.Common.Models;
using KellermanSoftware.NetEmailValidation;
using BedBrigade.Common.Constants;
using BedBrigade.SpeakIt;

namespace BedBrigade.Client.Components.Pages
{

    public partial class Index : ComponentBase, IDisposable
    {
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private ILoadImagesService _loadImagesService { get; set; }
        [Inject] private ICarouselService _carouselService { get; set; }
        [Inject] private IScheduleControlService _scheduleControlService { get; set; }
        [Inject] private IJSRuntime _js { get; set; }

        [Inject]
        private ILocationState _locationState { get; set; }
        [Inject] private IContentTranslationDataService _svcContentTranslation { get; set; }

        [Inject] private ILanguageService _svcLanguage { get; set; }

        [Inject] private ITranslationDataService _translateLogic { get; set; }

        [Parameter] public string? mylocation { get; set; }
        [Parameter] public string? mypageName { get; set; }

        const string defaultLocation = SeedConstants.SeedNationalName;
        const string defaultPageName = "Home";

        private string previousLocation = SeedConstants.SeedNationalName;
        private string previousPageName = defaultPageName;
        private string PreviousBodyContent = null;
        private string BodyContent = string.Empty;
        private string CurrentPageTitle = string.Empty;
        private string SorryPageUrl = String.Empty;

        protected override async Task OnInitializedAsync()
        {
            ValidateUrlParameters();

            string location = string.IsNullOrEmpty(mylocation) ? defaultLocation : mylocation;
            string pageName = string.IsNullOrEmpty(mypageName) ? defaultPageName : mypageName;
            await LoadLocationPage(location, pageName);
            _svcLanguage.LanguageChanged += OnLanguageChanged;
        }

        private async Task OnLanguageChanged(CultureInfo arg)
        {
            string location = string.IsNullOrEmpty(mylocation) ? defaultLocation : mylocation;
            string pageName = string.IsNullOrEmpty(mypageName) ? defaultPageName : mypageName;
            await LoadLocationPage(location, pageName);
            StateHasChanged();
        }

        public void Dispose()
        {
            _svcLanguage.LanguageChanged -= OnLanguageChanged;
        }

        protected override async Task OnParametersSetAsync()
        {
            ValidateUrlParameters();

            string location = string.IsNullOrEmpty(mylocation) ? defaultLocation : mylocation;
            string pageName = string.IsNullOrEmpty(mypageName) ? defaultPageName : mypageName;
            

            if (previousLocation.ToLower() != location.ToLower() || previousPageName.ToLower() != pageName.ToLower())
            {
                previousLocation = location;
                previousPageName = pageName;
                bool loaded=  await LoadLocationPage(location, pageName);

                if (loaded)
                {
                    _locationState.Location = location;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!String.IsNullOrEmpty(BodyContent) && BodyContent.Contains("jarallax"))
            {
                await _js.InvokeVoidAsync("BedBrigadeUtil.InitializeJarallax");
            }

            if (!String.IsNullOrEmpty(BodyContent) && BodyContent.Contains("carousel\""))
            {
                await _js.InvokeVoidAsync("BedBrigadeUtil.runCarousel", 3000);
            }
        }

        private async Task<bool> LoadLocationPage(string location, string pageName)
        {
            Log.Logger.Debug("Index.LoadLocationPage");
            bool found = false;
            try
            {
                var locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{location}");

                if (locationResponse.Success && locationResponse.Data != null)
                {
                    if (_svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
                    {
                        found= await LoadDefaultContent(location, pageName, locationResponse);
                    }
                    else
                    {
                        found = await LoadContentByLanguage(location, pageName, locationResponse);
                    }
                }

                if (!found)
                {
                    _navigationManager.NavigateTo(SorryPageUrl, true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"LoadLocationPage: {ex.Message}");
                throw;
            }

            return true;
        }

        private async Task<bool> LoadContentByLanguage(string location, string pageName, ServiceResponse<Location> locationResponse)
        {
            var contentResult = await _svcContentTranslation.GetAsync(pageName, locationResponse.Data.LocationId, _svcLanguage.CurrentCulture.Name);
            if (contentResult.Success)
            {
                var path = $"/{location}/pages/{pageName}";
                string html = _loadImagesService.SetImagesForHtml(path, contentResult.Data.ContentHtml);
                html = _carouselService.ReplaceCarousel(html);
                html = await _scheduleControlService.ReplaceScheduleControl(html, locationResponse.Data.LocationId);

                if (html != PreviousBodyContent)
                {
                    PreviousBodyContent = html;
                    BodyContent = html;
                }


                if (locationResponse.Data.LocationId == Defaults.NationalLocationId)
                {
                    string bedBrigade = await _translateLogic.GetTranslation("The Bed Brigade", _svcLanguage.CurrentCulture.Name);
                    string title = await _translateLogic.GetTranslation(contentResult.Data.Title, _svcLanguage.CurrentCulture.Name);
                    CurrentPageTitle = $"{bedBrigade} | {title}";
                }
                else
                {
                    string locationName = await _translateLogic.GetTranslation(locationResponse.Data.Name, _svcLanguage.CurrentCulture.Name);
                    string title = await _translateLogic.GetTranslation(contentResult.Data.Title, _svcLanguage.CurrentCulture.Name);
                    CurrentPageTitle = $"{locationName} | {title}";
                }
                return true;
            }

            return await LoadDefaultContent(location, pageName, locationResponse);
        }

        private async Task<bool> LoadDefaultContent(string location, string pageName, ServiceResponse<Location> locationResponse)
        {
            var contentResult = await _svcContent.GetAsync(pageName, locationResponse.Data.LocationId);
            if (contentResult.Success)
            {                       
                var path = $"/{location}/pages/{pageName}";
                string html = _loadImagesService.SetImagesForHtml(path, contentResult.Data.ContentHtml);
                html = _carouselService.ReplaceCarousel(html);
                html = await _scheduleControlService.ReplaceScheduleControl(html, locationResponse.Data.LocationId);

                if (html != PreviousBodyContent)
                {
                    PreviousBodyContent = html;
                    BodyContent = html;
                }

                if (locationResponse.Data.LocationId == Defaults.NationalLocationId)
                {
                    CurrentPageTitle = $"The Bed Brigade | {contentResult.Data.Title}";
                }
                else
                {
                    CurrentPageTitle = $"{locationResponse.Data.Name} | {contentResult.Data.Title}";
                }
            }
            else
            {
                _navigationManager.NavigateTo(SorryPageUrl, true);
                return false;
            }

            return true;
        }
        


        private async void ValidateUrlParameters()
        {
            bool isValidLocationRoute = await IsLocationRoute(StringUtil.IsNull(mylocation,""));
            var uri = new Uri(_navigationManager.Uri);
            var myUrlContent = new BedBrigade.Common.Logic.UrlContent();
            myUrlContent = UrlUtil.ValidateUrlContent(uri.ToString(), isValidLocationRoute);

           // Debug.WriteLine($"Full Page URL: {myUrlContent.FullUrl}");
           // Debug.WriteLine($"Parameterized URL?: {myUrlContent.IsParameterizedUrl.ToString()}");
           // Debug.WriteLine($"Positioning Parameters: {myUrlContent.PositioningCount}");
           // Debug.WriteLine($"Query Parameters: {myUrlContent.QueryCount}");
           // Debug.WriteLine($"Accepted Location Route: {myUrlContent.AcceptedLocationRoute}");
           // Debug.WriteLine($"Accepted Page Name: {myUrlContent.AcceptedPageName}");
           // Debug.WriteLine($"Final Sorry Page URL: {myUrlContent.SorryPageUrl}");

            mylocation = myUrlContent.AcceptedLocationRoute;
            mypageName = myUrlContent.AcceptedPageName;
            SorryPageUrl = myUrlContent.SorryPageUrl;


        } // Validate Location Parameter

        private async Task<bool> IsLocationRoute(string locationRoute)
        {
            bool IsValidLocation = false;
            locationRoute = "/" + locationRoute;

            var testLocation = await _svcLocation.GetLocationByRouteAsync(locationRoute);
            if (testLocation != null && testLocation.Success)
            {
                IsValidLocation = true;
            }

          //  Debug.WriteLine($"Location Parameter: {locationRoute} is {IsValidLocation}");

            return (IsValidLocation);
        } // iS location parameter
          


    } // razor class
} // namespace
