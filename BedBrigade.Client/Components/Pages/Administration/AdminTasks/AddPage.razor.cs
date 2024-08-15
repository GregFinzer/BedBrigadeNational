using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Data.Models;
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
    public string ErrorMessage { get; set; }
    
    public AddPageModel Model { get; set; } = new();


    protected override async Task OnInitializedAsync()
    {
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
            ErrorMessage = "A page with that name already exists for this location.";
            return;
        }

        var pageTemplate = await _svcTemplateDataService.GetByNameAsync(Defaults.DefaultPageTemplate);

        if (!pageTemplate.Success)
        {
            ErrorMessage = pageTemplate.Message;
            return;
        }

        var content = new Content
        {
            Name = Model.PageName,
            LocationId = Model.CurrentLocationId,
            ContentHtml = pageTemplate.Data.ContentHtml.Replace("%PageTitle%", Model.PageTitle),
            ContentType = BedBrigade.Common.Enums.ContentType.Body,
            Title = Model.PageTitle
        };

        var createResponse = await _svcContentDataService.CreateAsync(content);

        if (createResponse.Success)
        {
            _navigationManager.NavigateTo($"/administration/edit/editcontent/{Model.CurrentLocationId}/{Model.PageName}");
        }
        else
        {
            ErrorMessage = createResponse.Message;
        }
    }

    private void HandleCancel()
    {
        _navigationManager.NavigateTo("/administration/manage/pages");
    }
}