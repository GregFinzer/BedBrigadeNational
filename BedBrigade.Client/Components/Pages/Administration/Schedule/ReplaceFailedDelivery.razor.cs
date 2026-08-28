using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using Serilog;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Components.Pages.Administration.Schedule;

public partial class ReplaceFailedDelivery : ComponentBase
{
    [Inject] private NavigationManager _nav { get; set; } = default!;

    [Inject] private IScheduleDataService _scheduleDataService { get; set; } = default!;

    [Inject] private IBedRequestDataService _bedRequestDataService { get; set; } = default!;

    [Inject] private IBedRequestFailedDeliveryDataService BedRequestFailedDeliveryDataService { get; set; } = default!;
    
    [Inject] private ISendSmsLogic _sendSmsLogic { get; set; } = default!;
    [Inject] private IEmailBuilderService _emailBuilderService { get; set; } = default!;    
    [Inject] private ToastService ToastService { get; set; } = default!;
    
    [SupplyParameterFromQuery]
    public int? ScheduleId {  get; set; }
    
    [SupplyParameterFromQuery]
    public int? FailedBedRequestId { get; set; }
    
    [SupplyParameterFromQuery]
    public string? Status { get; set; }
    
    [SupplyParameterFromQuery]
    public int? CallRequestId { get; set; }
    
    private int WorkflowStep { get; set; }
    private const int PickSchedule = 0;
    private const int PickFailedDelivery = 1;
    private const int PickStatus = 2;
    private const int PickReplacement = 3;
    private const int CallReplacement = 4;
    private bool _chooseEventButtonDisabled = true;
    private const string BaseUrl = "/administration/schedule/replacefaileddelivery";
    
    public List<Common.Models.Schedule> FutureDeliverySchedules { get; set; } = new List<Common.Models.Schedule>();
    public List<Common.Models.BedRequest> BedRequestsForEvent { get; set; } = new List<Common.Models.BedRequest>();
    public Common.Models.BedRequest FailedBedRequest { get; set; } = new Common.Models.BedRequest();
    public List<Common.Models.BedRequest> ReplacementBedRequests { get; set; } = new List<Common.Models.BedRequest>();
    public Common.Models.BedRequest CallBedRequest { get; set; } = new Common.Models.BedRequest();
    
    private string SearchText { get; set; } = string.Empty;
    private string SearchTextReplace { get; set; } = string.Empty;
    private bool _isBusy = false;
    
    protected override async Task OnParametersSetAsync()
    {
        DetermineWorkflowStep();
        await LoadScheduleData();
        await LoadBedRequestsForEvent();
        await LoadFailedBedRequest();
        await LoadReplacementBedRequests();
        await LoadCallBedRequest();
    }

    private async Task LoadCallBedRequest()
    {
        if (CallRequestId.HasValue && CallRequestId.Value > 0)
        {
            var bedRequestResponse = await _bedRequestDataService.GetByIdAsync(CallRequestId.Value);

            if (bedRequestResponse.Success && bedRequestResponse.Data != null)
            {
                CallBedRequest = bedRequestResponse.Data;
            }
        }
    }

    private async Task LoadReplacementBedRequests()
    {
        if (FailedBedRequestId.HasValue 
            && FailedBedRequestId.Value > 0 
            && FailedBedRequest != null 
            && FailedBedRequest.BedRequestId > 0)
        {
            var bedRequestResponse = await BedRequestFailedDeliveryDataService.GetReplacementBedRequests(FailedBedRequest);
            
            if (bedRequestResponse.Success && bedRequestResponse.Data != null)
            {
                ReplacementBedRequests = bedRequestResponse.Data;
            }
        }
    }

    private async Task LoadFailedBedRequest()
    {
        if (FailedBedRequestId.HasValue)
        {
            var bedReqeuestResponse = await _bedRequestDataService.GetByIdAsync(FailedBedRequestId.Value);

            if (bedReqeuestResponse.Success && bedReqeuestResponse.Data != null)
            {
                FailedBedRequest = bedReqeuestResponse.Data;
            }
        }
    }

    private async Task LoadBedRequestsForEvent()
    {
        if (ScheduleId.HasValue)
        {
            var bedRequestResponse = await _bedRequestDataService.GetAllForScheduleId(ScheduleId.Value);

            if (bedRequestResponse.Success && bedRequestResponse.Data != null)
            {
                BedRequestsForEvent = bedRequestResponse.Data
                    .OrderBy(o => o.Team)
                    .ToList();
            }
        }
    }

    private async Task LoadScheduleData()
    {
        var scheduleResponse =
            await _scheduleDataService.GetFutureSchedulesByLocationId(_scheduleDataService.GetUserLocationId());
        
        if (scheduleResponse.Success && scheduleResponse.Data != null)
        {
            FutureDeliverySchedules = scheduleResponse.Data
                .Where(o => o.EventType == EventType.Delivery).ToList();
        }
    }

    private void DetermineWorkflowStep()
    {
        WorkflowStep = PickSchedule;
    
        if (CallRequestId.HasValue && CallRequestId.Value > 0)
            WorkflowStep = CallReplacement;
        else if (!string.IsNullOrWhiteSpace(Status))
            WorkflowStep = PickReplacement;
        else if (FailedBedRequestId.HasValue && FailedBedRequestId.Value > 0)
            WorkflowStep = PickStatus;
        else if (ScheduleId.HasValue  && ScheduleId.Value > 0)
            WorkflowStep = PickFailedDelivery;
    }


    private void ChooseEventNext()
    {
        string url = _nav.ToBaseRelativePath(_nav.Uri) + $"?scheduleId={ScheduleId}";
        _nav.NavigateTo(url);  
    }

    private void OnScheduleChange(ChangeEventArgs<int?, Common.Models.Schedule> obj)
    {
        ScheduleId = obj.Value;

        if (ScheduleId.HasValue)
        {
            _chooseEventButtonDisabled = false;
        }
        
        StateHasChanged();
    }

    private async Task HandleCalledClick()
    {
        if (CallBedRequest != null && CallBedRequest.BedRequestId > 0)
        {
            if (CallBedRequest.Notes == null || !CallBedRequest.Notes.Contains(Defaults.SameDayScheduleText))
            {
                string todayString = DateTime.Now.ToString("M/d/yy");
                CallBedRequest.Notes =
                    (CallBedRequest.Notes + $" LM {Defaults.SameDayScheduleText} {todayString}").Trim();
            }

            await _bedRequestDataService.UpdateAsync(CallBedRequest);
            string url = $"{BaseUrl}?scheduleId={ScheduleId}&failedBedRequestId={FailedBedRequestId}&status={Status}";
            _nav.NavigateTo(url);
        }
    }

    private async Task HandleConfirmedClick()
    {
        if (CallBedRequest == null 
            || CallBedRequest.BedRequestId == 0
            || FailedBedRequest == null
            || FailedBedRequest.BedRequestId == 0)
            return;

        if (_isBusy)
            return;

        try
        {
            _isBusy = true;
            if (!(await SaveCallBedRequest()))
                return;

            if (!(await SaveFailedBedRequest()))
                return;
            
            // Database updates succeeded, so send notifications.
            await Task.WhenAll(
                _sendSmsLogic.SendReplaceFailedDeliverySms(
                    FailedBedRequest, CallBedRequest),
                _emailBuilderService.SendReplaceFailedDeliveryEmail(
                    FailedBedRequest, CallBedRequest));
        }
        catch (Exception ex)
        {
            Log.Error(ex,"Error saving Confirm Bed Request Replacement");
            ToastService.Error("Error","Error saving Confirm Bed Request Replacement");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task<bool> SaveFailedBedRequest()
    {
        FailedBedRequest.Status = Enum.Parse<BedRequestStatus>(Status);
        string noteText = $" {Defaults.FailedDeliveryText} {DateTime.Now:M/d/yy}";
        if (FailedBedRequest.Notes == null || !FailedBedRequest.Notes.Contains(noteText))
            FailedBedRequest.Notes = (FailedBedRequest.Notes + noteText).Trim();
        
        FailedBedRequest.DeliveryDate = null;
        FailedBedRequest.Team = string.Empty;
        FailedBedRequest.ScheduleId = null;
        FailedBedRequest.DeliveryDate = null;
        
        var response = await _bedRequestDataService.UpdateAsync(FailedBedRequest);

        if (!response.Success)
        {
            Log.Error("Error saving FailedBedRequest: " + response.Message);
            ToastService.Error("Error saving FailedBedRequest",response.Message);
            return false;
        }

        return true;
    }

    private async Task<bool> SaveCallBedRequest()
    {
        if (!String.IsNullOrWhiteSpace(FailedBedRequest.Team))
        {
            CallBedRequest.Team = FailedBedRequest.Team;
        }

        string noteText = $" {Defaults.SameDayScheduleText} {DateTime.Now:M/d/yy} for TEAM {FailedBedRequest.Team}";
        
        if (CallBedRequest.Notes == null || !CallBedRequest.Notes.Contains(noteText))
            CallBedRequest.Notes = (FailedBedRequest.Notes + noteText).Trim();

        if (FailedBedRequest.ScheduleId != null)
        {
            CallBedRequest.ScheduleId = FailedBedRequest.ScheduleId;
        }

        if (FailedBedRequest.DeliveryDate != null)
        {
            CallBedRequest.DeliveryDate = FailedBedRequest.DeliveryDate;
        }

        CallBedRequest.Status = BedRequestStatus.Scheduled;
        var callResponse = await _bedRequestDataService.UpdateAsync(CallBedRequest);

        if (!callResponse.Success)
        {
            Log.Error("Error saving CallBedRequest: " + callResponse.Message);
            ToastService.Error("Error saving CallBedRequest",callResponse.Message);
            return false;
        }

        return true;
    }
}