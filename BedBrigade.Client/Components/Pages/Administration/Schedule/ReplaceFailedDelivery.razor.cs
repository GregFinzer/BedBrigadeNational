using System.Text;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Components.Pages.Administration.Schedule;

public partial class ReplaceFailedDelivery : ComponentBase
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    
    [Inject] private NavigationManager _nav { get; set; } = default!;

    [Inject] private IScheduleDataService _scheduleDataService { get; set; } = default!;

    [Inject] private IBedRequestDataService _bedRequestDataService { get; set; } = default!;

    [Inject] private IBedRequestFailedDeliveryDataService BedRequestFailedDeliveryDataService { get; set; } = default!;
    
    [Inject] private ISendSmsLogic _sendSmsLogic { get; set; } = default!;
    [Inject] private IEmailBuilderService _emailBuilderService { get; set; } = default!;    
    [Inject] private ToastService ToastService { get; set; } = default!;
    [Inject] private IContentDataService _contentDataService { get; set; } = default!;
    [Inject] private IMailMergeLogic _mailMergeLogic { get; set; } = default!;
    
    [SupplyParameterFromQuery]
    public int? ScheduleId {  get; set; }
    
    [SupplyParameterFromQuery]
    public int? FailedBedRequestId { get; set; }
    
    [SupplyParameterFromQuery]
    public string? Status { get; set; }
    
    [SupplyParameterFromQuery]
    public int? CallRequestId { get; set; }
    
    [SupplyParameterFromQuery]
    public bool? Replaced { get; set; }
    
    private int WorkflowStep { get; set; }
    private const int PickSchedule = 0;
    private const int PickFailedDelivery = 1;
    private const int PickStatus = 2;
    private const int PickReplacement = 3;
    private const int CallReplacement = 4;
    private const int ReplacedStep = 5;
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
    public string? WarningMessage { get; set; }
    public string? ErrorMessage { get; set; }
    private bool CopiedToClipboard { get; set; } = false;
    
    protected override async Task OnParametersSetAsync()
    {
        try
        {
            DetermineWorkflowStep();
            await LoadScheduleData();
            await LoadBedRequestsForEvent();
            await LoadFailedBedRequest();
            await LoadReplacementBedRequests();
            await LoadCallBedRequest();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ReplaceFailedDelivery.OnParametersSetAsync");
            ErrorMessage = "An error has occured: " + ex.Message;
        }
    }

    private async Task LoadCallBedRequest()
    {
        if (CallRequestId.HasValue && CallRequestId.Value > 0)
        {
            var bedRequestResponse = await _bedRequestDataService.GetByIdAsync(CallRequestId.Value);

            if (bedRequestResponse.Success && bedRequestResponse.Data != null)
            {
                if (WorkflowStep != ReplacedStep && bedRequestResponse.Data.Status == BedRequestStatus.Scheduled)
                {
                    WarningMessage = "This bed request has already been scheduled";
                }
                else
                {
                    CallBedRequest = bedRequestResponse.Data;
                }
            }
            else
            {
                Log.Error(bedRequestResponse.Message);
                ErrorMessage = "An error has occured: " + bedRequestResponse.Message;
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
            else
            {
                Log.Error(bedRequestResponse.Message);
                ErrorMessage = "An error has occured: " + bedRequestResponse.Message;
            }
        }
    }

    private async Task LoadFailedBedRequest()
    {
        if (FailedBedRequestId.HasValue && FailedBedRequestId.Value > 0)
        {
            var bedRequestResponse = await _bedRequestDataService.GetByIdAsync(FailedBedRequestId.Value);

            if (bedRequestResponse.Success && bedRequestResponse.Data != null)
            {
                if (WorkflowStep != ReplacedStep && bedRequestResponse.Data.Status != BedRequestStatus.Scheduled)
                {
                    WarningMessage =
                        $"The Bed Request for {bedRequestResponse.Data.FullName} has already been replaced.";
                }
                else
                {
                    FailedBedRequest = bedRequestResponse.Data;    
                }
            }
            else
            {
                Log.Error(bedRequestResponse.Message);
                ErrorMessage = "An error has occured: " + bedRequestResponse.Message;
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
            else
            {
                Log.Error(bedRequestResponse.Message);
                ErrorMessage = "An error has occured: " + bedRequestResponse.Message;
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
        else
        {
            Log.Error(scheduleResponse.Message);
            ErrorMessage = "An error has occured: " + scheduleResponse.Message;
        }	        
    }

    private void DetermineWorkflowStep()
    {
        WorkflowStep = PickSchedule;

        if (Replaced.HasValue && Replaced.Value)
            WorkflowStep = ReplacedStep;
        else if (CallRequestId.HasValue && CallRequestId.Value > 0)
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
            
            string url = $"{BaseUrl}?scheduleId={ScheduleId}&failedBedRequestId={FailedBedRequestId}&status={Status}&callRequestId={CallRequestId}&replaced=true";	
            _nav.NavigateTo(url);
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
            ErrorMessage = "Error saving FailedBedRequest: " + response.Message;
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
            ErrorMessage = "Error saving CallBedRequest: " + callResponse.Message;
            return false;
        }

        return true;
    }

    private async Task CopyToClipboard()
    {
        var templateResult = await _contentDataService.GetSingleByLocationAndContentType(CallBedRequest.LocationId, ContentType.ReplaceFailedDeliverySmsForm);

        if (!templateResult.Success || templateResult.Data == null || templateResult.Data.ContentHtml == null)
        {
            ErrorMessage = "Error getting template: " + templateResult.Message;
            return;
        }
        
        string template = templateResult.Data.ContentHtml;
        StringBuilder sb = new StringBuilder(template, template.Length * 2);
        sb = sb.Replace("%%FailedDeliveryRecipientName%%", $"{FailedBedRequest.FullName}");
        sb = _mailMergeLogic.ReplaceBedRequestFields(CallBedRequest, sb);
        
        await JsRuntime.InvokeVoidAsync(
            "navigator.clipboard.writeText",
            sb.ToString());

        CopiedToClipboard = true;
    }
}