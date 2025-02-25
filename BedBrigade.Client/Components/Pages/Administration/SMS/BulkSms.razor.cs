using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Components.Pages.Administration.SMS;

public partial class BulkSms : ComponentBase
{
    [Inject] private IJSRuntime _js { get; set; }
    [Inject] private IUserDataService _svcUserDataService { get; set; }
    [Inject] private ILocationDataService _svcLocationDataService { get; set; }
    [Inject] private IScheduleDataService _svcScheduleDataService { get; set; }
    [Inject] private ISmsQueueDataService _svcSmsQueueDataService { get; set; }

    public BulkSmsModel Model { get; set; } = new();
    private bool isSuccess;
    private bool isFailure;
    private bool showPlan;
    private string message;
    private bool isNationalAdmin;
    private User user;

    protected override async Task OnInitializedAsync()
    {
        Model.Locations = (await _svcLocationDataService.GetAllAsync()).Data;
        user = (await _svcUserDataService.GetCurrentLoggedInUser()).Data;
        Model.Body = string.Empty;
        Model.Schedules = (await _svcScheduleDataService.GetFutureSchedulesByLocationId(user.LocationId)).Data;
        Model.CurrentLocationId = user.LocationId;
        isNationalAdmin = user.LocationId == Defaults.NationalLocationId;

        if (isNationalAdmin)
        {
            Model.SmsRecipientOptions = EnumHelper.GetEnumNameValues<SmsRecipientOption>();
        }
        else
        {
            Model.SmsRecipientOptions = EnumHelper.GetEnumNameValues<SmsRecipientOption>()
                .Where(x => x.Value != SmsRecipientOption.Everyone).ToList();
        }

        Model.CurrentSmsRecipientOption = SmsRecipientOption.Myself;
        Model.ShowLocationDropdown = false;
        Model.ShowEventDropdown = false;
        await BuildPlan();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        //Collapse the mobile menu
        await _js.InvokeVoidAsync("AddRemoveClass.RemoveClass", "navbarResponsive", "show");
    }

    private async Task HandleValidSubmit()
    {
        var phoneNumbersToSend = await _svcSmsQueueDataService.GetPhoneNumbersToSend(Model.CurrentLocationId,
            Model.CurrentSmsRecipientOption, Model.CurrentScheduleId);
        var result = await _svcSmsQueueDataService.QueueBulkSms(Model.CurrentLocationId, phoneNumbersToSend.Data, Model.Body);
        if (result.Success)
        {
            ShowSuccess("Text Messages successfully queued.");
        }
        else
        {
            ShowFailure("Text Messages failed to queue. " + result.Message);
        }
    }

    private async void LocationChangeEvent(ChangeEventArgs<int, Location> args)
    {
        Model.CurrentLocationId = args.Value;
        Model.Schedules = (await _svcScheduleDataService.GetFutureSchedulesByLocationId(Model.CurrentLocationId)).Data;
        Model.CurrentScheduleId = 0;
        await BuildPlan();
        StateHasChanged();
    }

    private async Task BuildPlan()
    {
        message = (await _svcSmsQueueDataService.GetSendPlanMessage(Model.CurrentLocationId,
            Model.CurrentSmsRecipientOption, Model.CurrentScheduleId)).Data;
        showPlan = true;
    }

    private async void ScheduleChangeEvent(ChangeEventArgs<int, Common.Models.Schedule> args)
    {
        Model.CurrentScheduleId = args.Value;
        await BuildPlan();
        StateHasChanged();
    }

    private async void SmsRecipientChangeEvent(
        ChangeEventArgs<SmsRecipientOption, EnumNameValue<SmsRecipientOption>> args)
    {
        Model.CurrentSmsRecipientOption = args.Value;
        Model.ShowEventDropdown = Model.CurrentSmsRecipientOption.ToString().Contains("Event");
        Model.ShowLocationDropdown = isNationalAdmin &&
                                     (Model.CurrentSmsRecipientOption.ToString().Contains("Location")
                                      || Model.CurrentSmsRecipientOption.ToString().Contains("Event"));

        if (!Model.ShowLocationDropdown)
        {
            Model.CurrentLocationId = user.LocationId;
        }
        await BuildPlan();
        StateHasChanged();
    }

    public void ShowSuccess(string successMessage)
    {
        isSuccess = true;
        isFailure = false;
        showPlan = false;
        message = successMessage;
    }

    public void ShowFailure(string failureMessage)
    {
        isFailure = true;
        isSuccess = false;
        showPlan = false;
        message = failureMessage;
    }
}

