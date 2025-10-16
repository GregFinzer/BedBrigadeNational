using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using System.Linq;

namespace BedBrigade.Client.Components.Pages.Administration
{
    public partial class Dashboard : ComponentBase
    {
        [Inject] protected IScheduleDataService ScheduleService { get; set; } = default!;
        [Inject] protected IAuthService AuthService { get; set; } = default!;
        [Inject] protected IBedRequestDataService BedRequestService { get; set; } = default!;
        [Inject] protected IContactUsDataService ContactUsService { get; set; } = default!;
        [Inject] protected ISmsQueueDataService SmsQueueDataService  { get; set; } = default!;
        [Inject] protected ISignUpDataService SignUpDataService { get; set; } = default!;
        protected List<BedRequestDashboardRow>? BedRequestsDashboard { get; set; } = default!;
        protected List<Common.Models.Schedule>? Schedules { get; set; }
        protected int ContactsNeedingResponses { get; set; }
        protected List<SmsQueueSummary>? SmsQueueSummaries { get; set; }
        protected int UnreadMessages { get; set; }
        protected List<SignUp>? SignUps { get; set; }
        // Chart data for Bed Request History
        protected bool IsHistoryLoading { get; set; } = true;
        protected string CurrentYearLabel => DateTime.UtcNow.Year.ToString();
        protected string PrevYearLabel => (DateTime.UtcNow.Year - 1).ToString();
        protected string TwoYearsAgoLabel => (DateTime.UtcNow.Year - 2).ToString();
        protected List<ChartPoint>? SeriesCurrentYear { get; set; }
        protected List<ChartPoint>? SeriesPrevYear { get; set; }
        protected List<ChartPoint>? SeriesTwoYearsAgo { get; set; }
        // Chart data for Bed Delivery History
        protected bool IsDeliveryHistoryLoading { get; set; } = true;
        protected List<ChartPoint>? DeliverySeriesCurrentYear { get; set; }
        protected List<ChartPoint>? DeliverySeriesPrevYear { get; set; }
        protected List<ChartPoint>? DeliverySeriesTwoYearsAgo { get; set; }
        protected override async Task OnInitializedAsync()
        {
            int locationId = AuthService.LocationId;

            // Existing schedules
            var scheduleResponse = await ScheduleService.GetScheduleForMonthsAndLocation(locationId, 2);
            Schedules = scheduleResponse.Success ? scheduleResponse.Data : new List<Common.Models.Schedule>();

            // New bed requests
            var bedResponse = await BedRequestService.GetWaitingDashboard(locationId);
            BedRequestsDashboard = bedResponse.Success ? bedResponse.Data : new List<BedRequestDashboardRow>();

            // Contacts requested
            ContactsNeedingResponses = await ContactUsService.ContactsRequested(locationId);

            // SMS Queue
            var smsResponse = await SmsQueueDataService.GetSummaryForLocation(locationId);
            SmsQueueSummaries = smsResponse.Success ? smsResponse.Data : new List<SmsQueueSummary>();
            UnreadMessages = SmsQueueSummaries?.Sum(s => s.UnReadCount) ?? 0;

            // Sign-Ups for dashboard (through next two Saturdays)
            var signUpsResponse = await SignUpDataService.GetSignUpsForDashboard(locationId);
            SignUps = signUpsResponse.Success ? signUpsResponse.Data : new List<SignUp>();

            // Load chart data
            await LoadHistory(locationId);
            await LoadDeliveryHistory(locationId);
        }
        private async Task LoadHistory(int locationId)
        {
            try
            {
                IsHistoryLoading = true;
                var historyResponse = await BedRequestService.GetBedRequestHistory(locationId);
                var data = historyResponse.Success && historyResponse.Data != null
                    ? historyResponse.Data
                    : new List<BedRequestHistoryRow>();

                var currentYear = DateTime.UtcNow.Year;
                SeriesCurrentYear = BuildSeries(data, currentYear);
                SeriesPrevYear = BuildSeries(data, currentYear - 1);
                SeriesTwoYearsAgo = BuildSeries(data, currentYear - 2);
            }
            finally
            {
                IsHistoryLoading = false;
            }
        }

        private async Task LoadDeliveryHistory(int locationId)
        {
            try
            {
                IsDeliveryHistoryLoading = true;
                var historyResponse = await BedRequestService.GetBedDeliveryHistory(locationId);
                var data = historyResponse.Success && historyResponse.Data != null
                    ? historyResponse.Data
                    : new List<BedRequestHistoryRow>();

                var currentYear = DateTime.UtcNow.Year;
                DeliverySeriesCurrentYear = BuildSeries(data, currentYear);
                DeliverySeriesPrevYear = BuildSeries(data, currentYear - 1);
                DeliverySeriesTwoYearsAgo = BuildSeries(data, currentYear - 2);
            }
            finally
            {
                IsDeliveryHistoryLoading = false;
            }
        }

        private static List<ChartPoint> BuildSeries(List<BedRequestHistoryRow> rows, int year)
        {
            var months = Enumerable.Range(1, 12);
            var dict = rows.Where(r => r.Year == year).ToDictionary(r => r.Month, r => r.Count);
            return months.Select(m => new ChartPoint
            {
                Month = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m),
                Count = dict.TryGetValue(m, out var c) ? c : 0
            }).ToList();
        }

        protected class ChartPoint
        {
            public string Month { get; set; } = string.Empty;
            public int Count { get; set; }
        }
    }
}





