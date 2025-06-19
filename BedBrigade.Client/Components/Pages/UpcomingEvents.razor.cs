using BedBrigade.Client.Services;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using BedBrigade.SpeakIt;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;

namespace BedBrigade.Client.Components.Pages
{
    public partial class UpcomingEvents : ComponentBase
    {
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private ITranslateLogic _translateLogic { get; set; }
        [Inject] private ILocationState _locationState { get; set; }
        private List<Schedule>? DeliveryEvents { get; set; }
        private List<Schedule>? BuildEvents { get; set; }

        [Parameter] public string LocationRoute { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                var locationResponse = await _svcLocation.GetLocationByRouteAsync($"/{LocationRoute}");

                if (!locationResponse.Success)
                {
                    _navigationManager.NavigateTo($"/Sorry/{LocationRoute}/events");
                }

                var allEventsResponse =
                    await _svcSchedule.GetAvailableSchedulesByLocationId(locationResponse.Data.LocationId);

                if (!allEventsResponse.Success)
                {
                    _toastService.Error("Error", allEventsResponse.Message);
                    return;
                }

                _locationState.Location = LocationRoute;

                // Filter and sort events by date
                DeliveryEvents = allEventsResponse.Data
                    .Where(e => e.EventType == EventType.Delivery &&
                                e.EventStatus == EventStatus.Scheduled
                                && e.EventDateScheduled < DateTime.Now.AddMonths(7))
                    .OrderBy(e => e.EventDateScheduled)
                    .ToList();

                BuildEvents = allEventsResponse.Data
                    .Where(e => e.EventType == EventType.Build &&
                                e.EventStatus == EventStatus.Scheduled
                                && e.EventDateScheduled < DateTime.Now.AddMonths(7))
                    .OrderBy(e => e.EventDateScheduled)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"UpcomingEvents.OnInitializedAsync");
                _toastService.Error("Error", "An error occurred while loading upcoming events.");
            }
        }

        private async Task ScrollToBuilds()
        {
            await _js.InvokeVoidAsync("BedBrigadeUtil.ScrollToElementId", "upcoming-builds");
        }
    }
}
