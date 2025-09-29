using BedBrigade.Client.Components;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using StringUtil = BedBrigade.Common.Logic.StringUtil;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks;

public partial class RenamePage : ComponentBase
{
    [Inject] private IWebHostEnvironment _svcEnv { get; set; }
    [Inject] private ILocationDataService _svcLocationDataService { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    [Inject] private IContentDataService _svcContentDataService { get; set; }
    [Inject] private ToastService _toastService { get; set; }
    [Inject] private ITranslationProcessorDataService _svcTranslationProcessorDataService { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Parameter] public int LocationId { get; set; }
    [Parameter] public string ContentName { get; set; }
    public string ErrorMessage { get; set; }
    public Content? _originalContent;
    private string _locationRoute;
    private string _locationName;
    public RenamePageModel Model { get; set; } = new();


    protected override async Task OnInitializedAsync()
    {
        try
        {
            Log.Information($"{_svcAuth.UserName} went to the Rename Page");
            _originalContent = await GetOriginalContent();
            _locationRoute = await GetLocationRoute();

            if (_originalContent != null)
            {
                Model.PageTitle = _originalContent.Title;
                Model.PageName = _originalContent.Name;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing RenamePage component");
            _toastService.Error("Error", "An error occurred while loading the page data.");
            ErrorMessage = "An error occurred while loading the page data.";
        }
    }

    private async Task<string> GetLocationRoute()
    {
        var locationResponse = await _svcLocationDataService.GetByIdAsync(LocationId);

        if (!locationResponse.Success || locationResponse.Data == null)
        {
            Log.Error("RenamePage, Failed to load location data: " + locationResponse.Message);
            ErrorMessage = "Failed to load location data: " + locationResponse.Message;
            return string.Empty;
        }

        _locationName = locationResponse.Data.Name;
        return locationResponse.Data.Route;
    }

    private async Task<Content?> GetOriginalContent()
    {
        var contentResponse = await _svcContentDataService.GetAsync(ContentName, LocationId);
        if (!contentResponse.Success || contentResponse.Data == null)
        {
            Log.Error("RenamePage, Failed to load content data: " + contentResponse.Message);
            ErrorMessage = "Failed to load content data: " + contentResponse.Message;
            return null;
        }

        return contentResponse.Data;
    }

    private void UpdatePageName(ChangeEventArgs e)
    {
        string pageTitle = e.Value.ToString();
        string pageName = pageTitle.Replace(' ', '-');
        pageName = StringUtil.FilterAlphanumericAndDash(pageName);
        Model.PageName = pageName;
    }

    private void FilterPageName(ChangeEventArgs e)
    {
        string pageName = e.Value.ToString();
        pageName = pageName.Replace(" ", "-");
        pageName = StringUtil.FilterAlphanumericAndDash(pageName);
        Model.PageName = pageName;
    }

    private async Task HandleValidSubmit()
    {
        try
        {
            // If the name hasn't changed, just update the title
            if (_originalContent.Name == Model.PageName)
            {
                await JustUpdateTheTitle();

                return;
            }

            // If the name has changed, check if the new name is available
            var result = await _svcContentDataService.GetAsync(Model.PageName, LocationId);

            if (result.Success)
            {
                ErrorMessage = "A page with that name already exists for this location.";
                return;
            }

            await UpdateTitleAndNameAndRenameDirectory();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling valid submit in RenamePage component");
            ErrorMessage = "An error occurred while processing your request. Please try again later.";
        }
    }

    private async Task UpdateTitleAndNameAndRenameDirectory()
    {
        ServiceResponse<Content> updateResponse;
        //Rename directory
        var oldPath = $"{_svcEnv.ContentRootPath}/wwwroot/media{_locationRoute}/pages/{_originalContent.Name}";
        var newPath = $"{_svcEnv.ContentRootPath}/wwwroot/media{_locationRoute}/pages/{Model.PageName}";
        if (Directory.Exists(oldPath))
        {
            Directory.Move(oldPath, newPath);
        }

        //Replace Page Title
        _originalContent.Title = Model.PageTitle;
        _originalContent.ContentHtml = StringUtil.ReplaceTagValue(_originalContent.ContentHtml ?? string.Empty,
            "<h2>", "</h2>", Model.PageTitle);

        //Replace Page Name
        string originalMediaPath = $"/media{_locationRoute}/pages/{_originalContent.Name}/";
        string newMediaPath = $"/media{_locationRoute}/pages/{Model.PageName}/";
        _originalContent.Name = Model.PageName;
        _originalContent.ContentHtml = _originalContent.ContentHtml.Replace(originalMediaPath, newMediaPath);

        updateResponse = await _svcContentDataService.UpdateAsync(_originalContent);
        if (updateResponse.Success)
        {
            Log.Information($"{_svcAuth.UserName} rename page named {_originalContent.Name} to a name of {Model.PageName} for location {_locationName}");

            await _svcTranslationProcessorDataService.QueueContentTranslation(updateResponse.Data);

            _toastService.Success("Content Saved",
                $"Page updated " +
                $"for location {_locationRoute.TrimStart('/')} with name of {Model.PageName}");
            _navigationManager.NavigateTo($"/administration/manage/pages/{_originalContent.ContentType}");
        }
        else
        {
            Log.Error("RenamePage, Error updating content: " + updateResponse.Message);
            ErrorMessage = updateResponse.Message;
        }
    }

    private async Task JustUpdateTheTitle()
    {
        ServiceResponse<Content> updateResponse;
        _originalContent.Title = Model.PageTitle;
        _originalContent.ContentHtml = StringUtil.ReplaceTagValue(_originalContent.ContentHtml ?? string.Empty,
            "<h2>", "</h2>", Model.PageTitle);

        updateResponse = await _svcContentDataService.UpdateAsync(_originalContent);
        if (updateResponse.Success)
        {
            Log.Information($"{_svcAuth.UserName} updated the Page Title to {_originalContent.Title} for page named {_originalContent.Name} for location {_locationName}");
            _toastService.Success("Content Saved",
                $"Page updated for location {_locationRoute.TrimStart('/')} with name of {Model.PageName}");
            _navigationManager.NavigateTo($"/administration/manage/pages/{_originalContent.ContentType}");
        }
        else
        {
            Log.Error("RenamePage, Error updating the title: " + updateResponse.Message);
            ErrorMessage = updateResponse.Message;
        }
    }

    private void HandleCancel()
    {
        if (_originalContent == null)
        {
            _navigationManager.NavigateTo("/administration/manage/pages/body");
            return;
        }
        _navigationManager.NavigateTo($"/administration/manage/pages/{_originalContent.ContentType}");
    }
}