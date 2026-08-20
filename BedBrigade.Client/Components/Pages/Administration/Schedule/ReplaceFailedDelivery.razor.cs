using BedBrigade.Common.Enums;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Components.Pages.Administration.Schedule;

public partial class ReplaceFailedDelivery : ComponentBase
{
    [SupplyParameterFromQuery]
    public int? ScheduleId {  get; set; }
    
    [SupplyParameterFromQuery]
    public int? FailedBedRequestId { get; set; }
    
    [SupplyParameterFromQuery]
    public string? Status { get; set; }
    
    private int WorkflowStep { get; set; }
    private const int PickSchedule = 0;
    private const int PickFailedDelivery = 1;
    private const int PickConfirm = 2;
    private bool _chooseEventButtonDisabled = true;
    private const string BaseUrl = "/administration/schedule/replacefaileddelivery";
    
    public List<Common.Models.Schedule> FutureDeliverySchedules { get; set; } = new List<Common.Models.Schedule>();
    public List<Common.Models.BedRequest> BedRequests { get; set; } = new List<Common.Models.BedRequest>();
    public Common.Models.BedRequest FailedBedRequest { get; set; } = new Common.Models.BedRequest();
    
    private string SearchText { get; set; } = string.Empty;

    [Inject] private NavigationManager _nav { get; set; } = default!;

    [Inject] private IScheduleDataService _scheduleDataService { get; set; } = default!;

    [Inject] private IBedRequestDataService _bedRequestDataService { get; set; } = default!;
    
    protected override async Task OnParametersSetAsync()
    {
        DetermineWorkflowStep();
        await LoadScheduleData();
        await LoadBedRequests();
        await LoadFailedBedRequest();
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

    private async Task LoadBedRequests()
    {
        if (ScheduleId.HasValue)
        {
            var bedRequestResponse = await _bedRequestDataService.GetAllForScheduleId(ScheduleId.Value);

            if (bedRequestResponse.Success && bedRequestResponse.Data != null)
            {
                BedRequests = bedRequestResponse.Data
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
        
        if (FailedBedRequestId.HasValue && FailedBedRequestId.Value > 0)
            WorkflowStep = PickConfirm;
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
}