using BedBrigade.Client.Services;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;

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

        protected override async Task OnInitializedAsync()
        {
            string location = string.IsNullOrEmpty(mylocation) ? defaultLocation : mylocation;
            string pageName = string.IsNullOrEmpty(mypageName) ? defaultPageName : mypageName;
            await LoadLocationPage(location, pageName);
        }

        protected override async Task OnParametersSetAsync()
        {
            string location = string.IsNullOrEmpty(mylocation) ? defaultLocation : mylocation;
            string pageName = string.IsNullOrEmpty(mypageName) ? defaultPageName : mypageName;
            _locationState.Location = location;

            if (previousLocation.ToLower() != location.ToLower() || previousPageName.ToLower() != pageName.ToLower())
            {
                previousLocation = location;
                previousPageName = pageName;
                await LoadLocationPage(location, pageName);
            }
        }

        private async Task LoadLocationPage(string location, string pageName)
        {
            Log.Logger.Debug("Index.LoadLocationPage");

            try
            {
                var locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{location}");

                if (locationResponse.Success && locationResponse.Data != null)
                {
                    Console.WriteLine($"Location passed {location} Location {locationResponse.Data.LocationId} ");
                    var contentResult = await _svcContent.GetAsync(pageName, locationResponse.Data.LocationId);
                    Console.WriteLine($"Page: {pageName} Location: {locationResponse.Data.LocationId}");
                    if (contentResult.Success)
                    {
                        //string content = contentResult.Data;
                        var path = $"/{location}/pages/{pageName}";
                        string html = _loadImagesService.SetImagesForHtml(path, contentResult.Data.ContentHtml);
                        BodyContent = html;
                    }
                    else
                    {
                        _navigationManager.NavigateTo("/Sorry", true);
                    }
                }
                else
                {
                    _navigationManager.NavigateTo("/Sorry", true);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"LoadLocationPage: {ex.Message}");
                throw;
            }
        }
    }
}
