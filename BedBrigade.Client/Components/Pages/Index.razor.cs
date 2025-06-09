using BedBrigade.Client.Services;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using System.Globalization;
using BedBrigade.Common.Models;
using BedBrigade.Common.Constants;

namespace BedBrigade.Client.Components.Pages;


public partial class Index : ComponentBase, IDisposable
{
    [Inject] private ILocationDataService _svcLocation { get; set; }
    [Inject] private IContentDataService _svcContent { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    [Inject] private ILoadImagesService _loadImagesService { get; set; }
    [Inject] private ICarouselService _carouselService { get; set; }
    [Inject] private IScheduleControlService _scheduleControlService { get; set; }
    [Inject] private IJSRuntime _js { get; set; }
    [Inject] private ILocationState _locationState { get; set; }
    [Inject] private IContentTranslationDataService _svcContentTranslation { get; set; }

    [Inject] private ILanguageService _svcLanguage { get; set; }

    [Inject] private ITranslationDataService _translateLogic { get; set; }

    [Parameter] public string? LocationRoute { get; set; }
    [Parameter] public string? PageName { get; set; }

    private const string DefaultLocation = SeedConstants.SeedNationalName;
    private const string DefaultPageName = "Home";

    private string _currentLocation = string.Empty;
    private string _currentPageName = string.Empty;
    private string _previousLocation = string.Empty;
    private string _previousPageName = string.Empty;
    private string _previousBodyContent = null;
    private string _bodyContent = string.Empty;
    private string _currentPageTitle = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        PopulateCurrentLocationAndPageName();
        await LoadLocationPage(_currentLocation, _currentPageName);
        _svcLanguage.LanguageChanged += OnLanguageChanged;
    }

    private void PopulateCurrentLocationAndPageName()
    {
        if (string.IsNullOrEmpty(LocationRoute) && string.IsNullOrEmpty(PageName))
        {
            _currentLocation = DefaultLocation;
            _currentPageName = DefaultPageName;
        }
        else if (!string.IsNullOrEmpty(LocationRoute) && !string.IsNullOrEmpty(PageName))
        {
            _currentLocation = LocationRoute;
            _currentPageName = PageName;
        }
        else if (!string.IsNullOrEmpty(LocationRoute))
        {
            _currentLocation = DefaultLocation;
            _currentPageName = LocationRoute;
        }
        else
        {
            _currentLocation = string.IsNullOrEmpty(LocationRoute) ? DefaultLocation : LocationRoute;
            _currentPageName = string.IsNullOrEmpty(PageName) ? DefaultPageName : PageName;
        }
    }

    private async Task OnLanguageChanged(CultureInfo arg)
    {
        PopulateCurrentLocationAndPageName();
        await LoadLocationPage(_currentLocation, _currentPageName);
        StateHasChanged();
    }

    public void Dispose()
    {
        _svcLanguage.LanguageChanged -= OnLanguageChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        PopulateCurrentLocationAndPageName();

        _locationState.Location = _currentLocation;

        if (_previousLocation.ToLower() != _currentLocation.ToLower() || _previousPageName.ToLower() != _currentPageName.ToLower())
        {
            await LoadLocationPage(_currentLocation, _currentPageName);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!String.IsNullOrEmpty(_bodyContent) && _bodyContent.Contains("jarallax"))
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.InitializeJarallax");
        }

        if (!String.IsNullOrEmpty(_bodyContent) && _bodyContent.Contains("carousel\""))
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.runCarousel", 3000);
        }
    }

    private async Task LoadLocationPage(string location, string pageName)
    {
        Log.Logger.Debug("Index.LoadLocationPage");
        bool found = false;
        try
        {
            _previousLocation = location;
            _previousPageName = pageName;

            var locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{location}");

            if (locationResponse.Success && locationResponse.Data != null)
            {
                if (_svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
                {
                    found = await LoadDefaultContent(location, pageName, locationResponse);
                }
                else
                {
                    found = await LoadContentByLanguage(location, pageName, locationResponse);
                }
            }

            if (!found)
            {
                string sorryPageUrl = GetSorryPageUrl(locationResponse);
                _navigationManager.NavigateTo(sorryPageUrl, true);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, $"LoadLocationPage: {ex.Message}");
            throw;
        }
    }

    private string GetSorryPageUrl(ServiceResponse<Location> locationResponse)
    {
        if (!locationResponse.Success || locationResponse.Data == null)
        {
            return $"/Sorry/{LocationRoute}";
        }

        return $"/Sorry/{LocationRoute}/{PageName}";
    }

    private async Task<bool> LoadContentByLanguage(string location, string pageName,
        ServiceResponse<Location> locationResponse)
    {
        var contentResult = await _svcContentTranslation.GetAsync(pageName, locationResponse.Data.LocationId,
            _svcLanguage.CurrentCulture.Name);
        if (contentResult.Success)
        {
            var path = $"/{location}/pages/{pageName}";
            string html = await ReplaceHtmlControls(path, locationResponse, contentResult.Data.ContentHtml);

            if (html != _previousBodyContent)
            {
                _previousBodyContent = html;
                _bodyContent = html;
            }


            if (locationResponse.Data.LocationId == Defaults.NationalLocationId)
            {
                string bedBrigade =
                    await _translateLogic.GetTranslation("The Bed Brigade", _svcLanguage.CurrentCulture.Name);
                string title =
                    await _translateLogic.GetTranslation(contentResult.Data.Title, _svcLanguage.CurrentCulture.Name);
                _currentPageTitle = $"{bedBrigade} | {title}";
            }
            else
            {
                string locationName =
                    await _translateLogic.GetTranslation(locationResponse.Data.Name, _svcLanguage.CurrentCulture.Name);
                string title =
                    await _translateLogic.GetTranslation(contentResult.Data.Title, _svcLanguage.CurrentCulture.Name);
                _currentPageTitle = $"{locationName} | {title}";
            }

            return true;
        }

        return await LoadDefaultContent(location, pageName, locationResponse);
    }

    private async Task<bool> LoadDefaultContent(string location, string pageName,
        ServiceResponse<Location> locationResponse)
    {
        var contentResult = await _svcContent.GetAsync(pageName, locationResponse.Data.LocationId);
        if (contentResult.Success)
        {
            var path = $"/{location}/pages/{pageName}";
            string html = await ReplaceHtmlControls(path, locationResponse, contentResult.Data.ContentHtml);

            if (html != _previousBodyContent)
            {
                _previousBodyContent = html;
                _bodyContent = html;
            }

            if (locationResponse.Data.LocationId == Defaults.NationalLocationId)
            {
                _currentPageTitle = $"The Bed Brigade | {contentResult.Data.Title}";
            }
            else
            {
                _currentPageTitle = $"{locationResponse.Data.Name} | {contentResult.Data.Title}";
            }
        }
        else
        {
            string sorryPageUrl = GetSorryPageUrl(locationResponse);
            _navigationManager.NavigateTo(sorryPageUrl, true);
            return false;
        }

        return true;
    }

    private async Task<string> ReplaceHtmlControls(string path, ServiceResponse<Location> locationResponse, string html)
    {
        html = _loadImagesService.SetImagesForHtml(path, html);
        html = _carouselService.ReplaceCarousel(html);
        html = await _scheduleControlService.ReplaceScheduleControl(html, locationResponse.Data.LocationId);
        return html;
    }



} 

