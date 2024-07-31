using BedBrigade.Client.Components;
using BedBrigade.Common;
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks;

public partial class RenamePage : ComponentBase
{
    [Inject] private IWebHostEnvironment _svcEnv { get; set; }
    [Inject] private ILocationDataService _svcLocationDataService { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    [Inject] private IContentDataService _svcContentDataService { get; set; }
    [Inject] private ToastService _toastService { get; set; }
    [Parameter] public string LocationId { get; set; }
    [Parameter] public string ContentName { get; set; }
    public string ErrorMessage { get; set; }
    public Content _originalContent;
    private int _originalLocationId;
    private string _locationRoute;
    public RenamePageModel Model { get; set; } = new();


    protected override async Task OnInitializedAsync()
    {
        int _originalLocationId;

        if (!int.TryParse(LocationId, out _originalLocationId))
        {
            _toastService.Error("Error",
                $"Could not parse location as integer: {LocationId}");
            return;
        }

        _originalContent =  (await _svcContentDataService.GetAsync(ContentName, _originalLocationId)).Data;
        _locationRoute = (await _svcLocationDataService.GetByIdAsync(_originalLocationId)).Data.Route;
        Model.PageTitle = _originalContent.Title;
        Model.PageName = _originalContent.Name;
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
        ServiceResponse<Content> updateResponse;

        // If the name hasn't changed, just update the title
        if (_originalContent.Name == Model.PageName)
        {
            await JustUpdateTheTitle();

            return;
        }

        // If the name has changed, check if the new name is available
        var result = await _svcContentDataService.GetAsync(Model.PageName, _originalLocationId);

        if (result.Success)
        {
            ErrorMessage = "A page with that name already exists for this location.";
            return;
        }

        await UpdateTitleAndNameAndRenameDirectory();
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
            _toastService.Success("Content Saved",
                $"Page updated " +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"" +
                $"for location {_locationRoute.TrimStart('/')} with name of {Model.PageName}");
            _navigationManager.NavigateTo("/administration/manage/pages");
        }
        else
        {
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
            _toastService.Success("Content Saved",
                $"Page updated for location {_locationRoute.TrimStart('/')} with name of {Model.PageName}");
            _navigationManager.NavigateTo("/administration/manage/pages");
        }
        else
        {
            ErrorMessage = updateResponse.Message;
        }
    }

    private void HandleCancel()
    {
        _navigationManager.NavigateTo("/administration/manage/pages");
    }
}