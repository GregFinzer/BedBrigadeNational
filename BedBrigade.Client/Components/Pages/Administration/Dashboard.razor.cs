using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components.Pages.Administration
{
    public partial class Dashboard : ComponentBase
    {
        [Inject] protected IScheduleDataService ScheduleService { get; set; } = default!;
        [Inject] protected IAuthService AuthService { get; set; } = default!;

        protected List<Common.Models.Schedule>? Schedules { get; set; }

        protected override async Task OnInitializedAsync()
        {
            int locationId = AuthService.LocationId;
            var response = await ScheduleService.GetScheduleForMonthsAndLocation(locationId, 3);
            if (response.Success)
            {
                Schedules = response.Data;
            }
            else
            {
                Schedules = new List<Common.Models.Schedule>();
            }
        }
    }
}





