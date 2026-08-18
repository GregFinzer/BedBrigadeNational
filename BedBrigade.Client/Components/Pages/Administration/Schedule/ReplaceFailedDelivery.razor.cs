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
    private int WorkflowStep { get; set; }
    private const int PickSchedule = 0;
    private const int PickFailedDelivery = 1;
    private bool _chooseEventButtonDisabled = true;
    private string _baseUrl = string.Empty;
    
    public List<Common.Models.Schedule> FutureDeliverySchedules { get; set; } = new List<Common.Models.Schedule>();
    public List<Common.Models.BedRequest> BedRequests { get; set; } = new List<Common.Models.BedRequest>();
    private string SearchText { get; set; } = string.Empty;

    [Inject] private NavigationManager _nav { get; set; } = default!;

    [Inject] private IScheduleDataService _scheduleDataService { get; set; } = default!;

    [Inject] private IBedRequestDataService _bedRequestDataService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _baseUrl = _nav.Uri;
        DetermineWorkflowStep();
        await LoadScheduleData();
        await LoadBedRequests();
    }

    protected override async Task OnParametersSetAsync()
    {
        _baseUrl = _nav.Uri;
        DetermineWorkflowStep();
        await LoadScheduleData();
        await LoadBedRequests();
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
        if (ScheduleId.HasValue)
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