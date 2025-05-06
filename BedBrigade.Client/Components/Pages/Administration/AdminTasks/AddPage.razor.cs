using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;

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
        Model.Locations = (await _svcLocationDataService.GetAllAsync()).Data;
        var user = (await _svcUserDataService.GetCurrentLoggedInUser()).Data;
        Model.CurrentLocationId = user.LocationId;
        Model.IsNationalAdmin = Model.CurrentLocationId == Defaults.NationalLocationId;
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

            if (!pageTemplate.Success)
            {
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
            ErrorMessage = createResponse.Message;
        }
    }

    private void HandleCancel()
    {
        _navigationManager.NavigateTo($"/administration/manage/pages/{SelectedContentType.ToString()}");
    }
}