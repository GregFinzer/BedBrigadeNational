using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using StringUtil = BedBrigade.Common.Logic.StringUtil;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks;

public partial class AddPage : ComponentBase
{
    [Inject] private ILocationDataService _svcLocationDataService { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    [Inject] private IContentDataService _svcContentDataService { get; set; }
    [Inject] private ITemplateDataService _svcTemplateDataService { get; set; }
    [Inject] private ITranslationProcessorDataService _svcTranslationProcessorDataService { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private ToastService _toastService { get; set; }
    [Parameter]
    public string ContentTypeString { get; set; }

    public string ErrorMessage { get; set; }
    
    public AddPageModel Model { get; set; } = new();

    public string? Title { get; set; }
    public ContentType SelectedContentType { get; set; }
    public string SingularName { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Log.Information($"{_svcAuth.UserName} went to the AddPage Page");
            SelectedContentType = Enum.Parse<BedBrigade.Common.Enums.ContentType>(ContentTypeString, true);
            SingularName = Pluralization.MakeSingular(SelectedContentType.ToString());
            Title = $"Add {SingularName}";
            await LoadLocations();
            Model.CurrentLocationId = _svcAuth.LocationId;
            Model.IsNationalAdmin = _svcAuth.IsNationalAdmin;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing AddPage component");
            _toastService.Error("Error", "An error occurred while loading the page data.");
            ErrorMessage = "An error occurred while loading the page data.";
        }
    }



    private async Task LoadLocations()
    {
        var locationsResult = await _svcLocationDataService.GetActiveLocations();
        if (!locationsResult.Success && locationsResult.Data != null)
        {
            Log.Error("AddPage, Failed to load locations: " +  locationsResult.Message);
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
        try
        {
            var result = await _svcContentDataService.GetAsync(Model.PageName, Model.CurrentLocationId);

            if (result.Success)
            {
                ErrorMessage = $"A {SingularName} with that name already exists for this location.";
                return;
            }

            Content? content = await BuildContent();

            if (content == null)
                return;

            var createResponse = await _svcContentDataService.CreateAsync(content);

            if (createResponse.Success)
            {
                string locationName = Model.Locations.FirstOrDefault(l => l.LocationId == Model.CurrentLocationId)?.Name ?? "Unknown Location";
                await _svcTranslationProcessorDataService.QueueContentTranslation(createResponse.Data);
                _navigationManager.NavigateTo($"/administration/edit/editcontent/{Model.CurrentLocationId}/{Model.PageName}");
            }
            else
            {
                Log.Error("AddPage, Could not add page: " + createResponse.Message);
                ErrorMessage = createResponse.Message;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling valid submit in AddPage component");
            ErrorMessage = "An error occurred while processing your request. Please try again later.";
        }
    }

    private async Task<Content?> BuildContent()
    {
        Content content;

        if (SelectedContentType == ContentType.Body)
        {
            var pageTemplate = await _svcTemplateDataService.GetByNameAsync(Defaults.DefaultPageTemplate);

            if (!pageTemplate.Success || pageTemplate.Data == null)
            {
                Log.Error("Could not find default page template: " + pageTemplate.Message);
                ErrorMessage = pageTemplate.Message;
                return null;
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

        return content;
    }

    private void HandleCancel()
    {
        _navigationManager.NavigateTo($"/administration/manage/pages/{SelectedContentType.ToString()}");
    }
}