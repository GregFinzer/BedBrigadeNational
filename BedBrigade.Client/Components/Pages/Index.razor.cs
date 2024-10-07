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
using KellermanSoftware.NetEmailValidation;

namespace BedBrigade.Client.Components.Pages
{

    public partial class Index : ComponentBase
    {
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private ILoadImagesService _loadImagesService { get; set; }
        [Inject] private IJSRuntime _js { get; set; }

        [Inject]
        private ILocationState _locationState { get; set; }

        [Parameter] public string? mylocation { get; set; }
        [Parameter] public string? mypageName { get; set; }

        const string defaultLocation = SeedConstants.SeedNationalName;
        const string defaultPageName = "Home";

        private string previousLocation = SeedConstants.SeedNationalName;
        private string previousPageName = defaultPageName;
        private string BodyContent = string.Empty;
        private string SorryPageUrl = String.Empty;
       
        protected override async Task OnInitializedAsync()
        {
            ValidateUrlParameters();

            string location = string.IsNullOrEmpty(mylocation) ? defaultLocation : mylocation;
            string pageName = string.IsNullOrEmpty(mypageName) ? defaultPageName : mypageName;
            await LoadLocationPage(location, pageName);
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
        }

        private async Task<bool> LoadLocationPage(string location, string pageName)
        {
            Log.Logger.Debug("Index.LoadLocationPage");

            try
            {
                var locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{location}");

                if (locationResponse.Success && locationResponse.Data != null)
                {
                    //Debug.WriteLine($"Location passed {location} Location {locationResponse.Data.LocationId} ");
                    var contentResult = await _svcContent.GetAsync(pageName, locationResponse.Data.LocationId);
                    //Debug.WriteLine($"Page: {pageName} Location: {locationResponse.Data.LocationId}");
                    if (contentResult.Success)
                    {                       
                        var path = $"/{location}/pages/{pageName}";
                        string html = _loadImagesService.SetImagesForHtml(path, contentResult.Data.ContentHtml);
                        BodyContent = html;
                    }
                    else
                    {
                        //_navigationManager.NavigateTo($"/Sorry/{location}/{pageName}", true);
                        _navigationManager.NavigateTo(SorryPageUrl, true);
                        return false;
                    }
                }
                else
                {
                    //_navigationManager.NavigateTo($"/Sorry/{location}", true);
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
        } // Load Location Page


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
