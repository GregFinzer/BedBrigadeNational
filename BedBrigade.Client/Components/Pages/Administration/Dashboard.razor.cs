using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components.Pages.Administration
{
    public partial class Dashboard : ComponentBase
    {
        [Inject] protected IScheduleDataService ScheduleService { get; set; } = default!;
        [Inject] protected IAuthService AuthService { get; set; } = default!;
        [Inject] protected IBedRequestDataService BedRequestService { get; set; } = default!;

        protected List<BedRequestDashboardRow>? BedRequestsDashboard { get; set; }

        protected List<Common.Models.Schedule>? Schedules { get; set; }

        protected override async Task OnInitializedAsync()
        {
            int locationId = AuthService.LocationId;

            // Existing schedules
            var scheduleResponse = await ScheduleService.GetScheduleForMonthsAndLocation(locationId, 2);
            Schedules = scheduleResponse.Success ? scheduleResponse.Data : new List<Common.Models.Schedule>();

            // New bed requests
            var bedResponse = await BedRequestService.GetWaitingDashboard(locationId);
            BedRequestsDashboard = bedResponse.Success ? bedResponse.Data : new List<BedRequestDashboardRow>();
        }
    }
}





