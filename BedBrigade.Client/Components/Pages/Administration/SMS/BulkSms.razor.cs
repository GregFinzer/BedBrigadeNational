using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Components.Pages.Administration.SMS;

public partial class BulkSms : ComponentBase
{
    [Inject] private IJSRuntime _js { get; set; }
    [Inject] private IUserDataService _svcUserDataService { get; set; }
    [Inject] private ILocationDataService _svcLocationDataService { get; set; }
    [Inject] private IScheduleDataService _svcScheduleDataService { get; set; }
    [Inject] private ISmsQueueDataService _svcSmsQueueDataService { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private ToastService _toastService { get; set; }
    public BulkSmsModel Model { get; set; } = new();
    private bool isSuccess;
    private bool isFailure;
    private bool showPlan;
    private string message;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Log.Information($"{_svcAuth.UserName} went to the Bulk SMS Page");
            await LoadLocations();
            Model.Body = string.Empty;
            await LoadSchedules();
            Model.CurrentLocationId = _svcAuth.LocationId;

            if (_svcAuth.IsNationalAdmin)
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
        catch (Exception ex)
        {
            Log.Error(ex, "Bulk SMS OnInitializedAsync");
            _toastService.Error("Error", $"An error occurred while initializing the page: {ex.Message}");
        }
    }

    private async Task LoadSchedules()
    {
        var scheduleResponse = await _svcScheduleDataService.GetAvailableSchedulesByLocationId(_svcAuth.LocationId);

        if (!scheduleResponse.Success || scheduleResponse.Data == null)
        {
            Log.Error("Error loading schedules: {Message}", scheduleResponse.Message);
            _toastService.Error("Error", $"An error occurred while loading schedules: {scheduleResponse.Message}");
            return;
        }
        Model.Schedules = scheduleResponse.Data;
    }

    private async Task LoadLocations()
    {
        var locationResponse = await _svcLocationDataService.GetActiveLocations();

        if (!locationResponse.Success || locationResponse.Data == null)
        {
            Log.Error("Error loading locations: {Message}", locationResponse.Message);
            _toastService.Error("Error", $"An error occurred while loading locations: {locationResponse.Message}");
            return;
        }

        Model.Locations = locationResponse.Data;
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
            Log.Information($"{_svcAuth.UserName} Bulk SMS successfully queued some messages");
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
        Model.ShowLocationDropdown = _svcAuth.IsNationalAdmin &&
                                     (Model.CurrentSmsRecipientOption.ToString().Contains("Location")
                                      || Model.CurrentSmsRecipientOption.ToString().Contains("Event"));

        if (!Model.ShowLocationDropdown)
        {
            Model.CurrentLocationId = _svcAuth.LocationId;
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

