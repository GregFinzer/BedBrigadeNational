using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks;

public partial class AddPage : ComponentBase
{
    [Inject] private IUserDataService _svcUserDataService { get; set; }
    [Inject] private ILocationDataService _svcLocationDataService { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    [Inject] private IContentDataService _svcContentDataService { get; set; }
    [Inject] private ITemplateDataService _svcTemplateDataService { get; set; }
    [Inject] private ITranslationProcessorDataService _svcTranslationProcessorDataService { get; set; }

    [Parameter]
    public string ContentTypeString { get; set; }

    public string ErrorMessage { get; set; }
    
    public AddPageModel Model { get; set; } = new();

    public string? Title { get; set; }
    public ContentType SelectedContentType { get; set; }
    public string SingularName { get; set; }

    protected override async Task OnInitializedAsync()
    {
        SelectedContentType = Enum.Parse<BedBrigade.Common.Enums.ContentType>(ContentTypeString, true);
        SingularName = Pluralization.MakeSingular(SelectedContentType.ToString());
        Title = $"Add {SingularName}";
        await LoadLocations();
        var user = await GetCurrentUser();

        if (user != null)
        {
            Model.CurrentLocationId = user.LocationId;
            Model.IsNationalAdmin = Model.CurrentLocationId == Defaults.NationalLocationId;
        }
    }

    private async Task<User?> GetCurrentUser()
    {
        var userResult = await _svcUserDataService.GetCurrentLoggedInUser();

        if (!userResult.Success || userResult.Data == null)
        {
            Log.Error(userResult.Message);
            ErrorMessage = "Failed to load user data: " + userResult.Message;
            return null;
        }
        return userResult.Data;
    }

    private async Task LoadLocations()
    {
        var locationsResult = await _svcLocationDataService.GetAllAsync();
        if (!locationsResult.Success && locationsResult.Data != null)
        {
            Log.Error(locationsResult.Message);
            ErrorMessage = "Failed to load locations: " + locationsResult.Message;
            return;
        }
        Model.Locations = locationsResult.Data;
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
        var result = await _svcContentDataService.GetAsync(Model.PageName, Model.CurrentLocationId);

        if (result.Success)
        {
            ErrorMessage = $"A {SingularName} with that name already exists for this location.";
            return;
        }

        Content content;

        if (SelectedContentType == ContentType.Body)
        {
            var pageTemplate = await _svcTemplateDataService.GetByNameAsync(Defaults.DefaultPageTemplate);

            if (!pageTemplate.Success || pageTemplate.Data == null)
            {
                Log.Error("Could not find default page template: " + pageTemplate.Message);
                ErrorMessage = pageTemplate.Message;
                return;
            }

            content = new Content
            {
                Name = Model.PageName,
                LocationId = Model.CurrentLocationId,
                ContentHtml = pageTemplate.Data.ContentHtml.Replace("%PageTitle%", Model.PageTitle),
                ContentType = SelectedContentType,
                Title = Model.PageTitle
            };
        }
        else
        {
            content = new Content
            {
                Name = Model.PageName,
                LocationId = Model.CurrentLocationId,
                ContentHtml = string.Empty,
                ContentType = SelectedContentType,
                Title = Model.PageTitle
            };
        }

        var createResponse = await _svcContentDataService.CreateAsync(content);

        if (createResponse.Success)
        {
            await _svcTranslationProcessorDataService.QueueContentTranslation(createResponse.Data);
            _navigationManager.NavigateTo($"/administration/edit/editcontent/{Model.CurrentLocationId}/{Model.PageName}");
        }
        else
        {
            Log.Error("Could not add page: " + createResponse.Message);
            ErrorMessage = createResponse.Message;
        }
    }

    private void HandleCancel()
    {
        _navigationManager.NavigateTo($"/administration/manage/pages/{SelectedContentType.ToString()}");
    }
}