using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using System.Linq;
using System.Security.Cryptography;

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
        [Inject] protected ILocationDataService LocationDataService { get; set; } = default!;
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
        protected List<ChartPoint>? BedRequestSeriesCurrentYear { get; set; }
        protected List<ChartPoint>? BedRequestSeriesPrevYear { get; set; }
        protected List<ChartPoint>? BedRequestSeriesTwoYearsAgo { get; set; }
        // Chart data for Bed Delivery History
        protected bool IsDeliveryHistoryLoading { get; set; } = true;
        protected List<ChartPoint>? DeliverySeriesCurrentYear { get; set; }
        protected List<ChartPoint>? DeliverySeriesPrevYear { get; set; }
        protected List<ChartPoint>? DeliverySeriesTwoYearsAgo { get; set; }
        protected string LocationName { get; set; }

        // Statistics for Bed Requests (derived from BedRequestSeriesCurrentYear/Prev/TwoYearsAgo)
        protected int CurrentYearYtdTotal { get; set; }
        protected int PrevYearTotal { get; set; }
        protected int TwoYearsAgoTotal { get; set; }
        protected int MinBedsPerMonth { get; set; }
        protected int MaxBedsPerMonth { get; set; }
        protected double AverageBedsPerMonth { get; set; }
        protected double AverageBedsPerWeek { get; set; }

        // Statistics for Bed Deliveries (derived from DeliverySeriesCurrentYear/Prev/TwoYearsAgo)
        protected int DeliveryCurrentYearYtdTotal { get; set; }
        protected int DeliveryPrevYearTotal { get; set; }
        protected int DeliveryTwoYearsAgoTotal { get; set; }
        protected int DeliveryMinBedsPerMonth { get; set; }
        protected int DeliveryMaxBedsPerMonth { get; set; }
        protected double DeliveryAverageBedsPerMonth { get; set; }
        protected double DeliveryAverageBedsPerWeek { get; set; }
        protected int LocationId { get; set; }
        protected string EstimatedWaitingTime { get; set; }
        protected override async Task OnInitializedAsync()
        {
            LocationId= AuthService.LocationId;
            LocationName = (await LocationDataService.GetByIdAsync(LocationId))?.Data?.Name ?? "Unknown Location";

            // Existing schedules
            var scheduleResponse = await ScheduleService.GetScheduleForMonthsAndLocation(LocationId, 2);
            Schedules = scheduleResponse.Success ? scheduleResponse.Data : new List<Common.Models.Schedule>();

            // New bed requests
            var bedResponse = await BedRequestService.GetWaitingDashboard(LocationId);
            BedRequestsDashboard = bedResponse.Success ? bedResponse.Data : new List<BedRequestDashboardRow>();

            // Contacts requested
            ContactsNeedingResponses = await ContactUsService.ContactsRequested(LocationId);

            // SMS Queue
            var smsResponse = await SmsQueueDataService.GetSummaryForLocation(LocationId);
            SmsQueueSummaries = smsResponse.Success ? smsResponse.Data : new List<SmsQueueSummary>();
            UnreadMessages = SmsQueueSummaries?.Sum(s => s.UnReadCount) ?? 0;

            // Sign-Ups for dashboard (through next two Saturdays)
            var signUpsResponse = await SignUpDataService.GetSignUpsForDashboard(LocationId);
            SignUps = signUpsResponse.Success ? signUpsResponse.Data : new List<SignUp>();

            // Load chart data
            await LoadBedRequestHistory(LocationId);
            await LoadDeliveryHistory(LocationId);
            EstimatedWaitingTime = (await BedRequestService.GetEstimatedWaitTime(LocationId))?.Data ?? "Unknown";
        }
        private async Task LoadBedRequestHistory(int locationId)
        {
            try
            {
                IsHistoryLoading = true;
                var historyResponse = await BedRequestService.GetBedRequestHistory(locationId);
                var data = historyResponse.Success && historyResponse.Data != null
                    ? historyResponse.Data
                    : new List<BedRequestHistoryRow>();

                var currentYear = DateTime.UtcNow.Year;
                BedRequestSeriesCurrentYear = BuildSeries(data, currentYear);
                BedRequestSeriesPrevYear = BuildSeries(data, currentYear - 1);
                BedRequestSeriesTwoYearsAgo = BuildSeries(data, currentYear - 2);

                // Compute statistics based on the series
                ComputeBedRequestStatistics();
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

                // Compute statistics for deliveries
                ComputeDeliveryStatistics();
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
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;

            return months.Select(m => new ChartPoint
            {
                Month = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m),
                // For the current year, do not plot future months: set Count to null so the chart skips them
                Count = (year == currentYear && m > currentMonth)
                    ? (int?)null
                    : dict.TryGetValue(m, out var c) ? (int?)c : 0
            }).ToList();
        }

        private void ComputeBedRequestStatistics()
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var prevYear = currentYear - 1;

            // Helper enumerables (coalesce nulls to zero)
            var ytdCounts = BedRequestSeriesCurrentYear?.Take(currentMonth).Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var prevYearCounts = BedRequestSeriesPrevYear?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var twoYearsAgoCounts = BedRequestSeriesTwoYearsAgo?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();

            // Totals (unchanged)
            CurrentYearYtdTotal = ytdCounts.Sum();
            PrevYearTotal = prevYearCounts.Sum();
            TwoYearsAgoTotal = twoYearsAgoCounts.Sum();

            // Combined current year YTD + previous year for statistics
            var combinedCounts = prevYearCounts.Concat(ytdCounts).ToList();
            var nonZero = combinedCounts.Where(c => c > 0).ToList();

            MinBedsPerMonth = nonZero.Any() ? nonZero.Min() : 0;
            MaxBedsPerMonth = combinedCounts.Any() ? combinedCounts.Max() : 0;
            AverageBedsPerMonth = nonZero.Any() ? nonZero.Average() : 0.0;

            // Average per week over months that have data (exclude zeros)
            double totalWeeks = 0.0;
            int totalBedsNonZero = 0;

            if (BedRequestSeriesPrevYear != null)
            {
                for (int m = 1; m <= 12; m++)
                {
                    var count = BedRequestSeriesPrevYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        totalWeeks += DateTime.DaysInMonth(prevYear, m) / 7.0;
                        totalBedsNonZero += count;
                    }
                }
            }
            if (BedRequestSeriesCurrentYear != null)
            {
                for (int m = 1; m <= currentMonth; m++)
                {
                    var count = BedRequestSeriesCurrentYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        totalWeeks += DateTime.DaysInMonth(currentYear, m) / 7.0;
                        totalBedsNonZero += count;
                    }
                }
            }

            AverageBedsPerWeek = totalWeeks > 0 ? totalBedsNonZero / totalWeeks : 0.0;
        }

        private void ComputeDeliveryStatistics()
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var prevYear = currentYear - 1;

            // Helper enumerables for deliveries (coalesce nulls to zero)
            var ytdCounts = DeliverySeriesCurrentYear?.Take(currentMonth).Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var prevYearCounts = DeliverySeriesPrevYear?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var twoYearsAgoCounts = DeliverySeriesTwoYearsAgo?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();

            // Totals (unchanged)
            DeliveryCurrentYearYtdTotal = ytdCounts.Sum();
            DeliveryPrevYearTotal = prevYearCounts.Sum();
            DeliveryTwoYearsAgoTotal = twoYearsAgoCounts.Sum();

            // Combined current year YTD + previous year for statistics
            var combinedCounts = prevYearCounts.Concat(ytdCounts).ToList();
            var nonZero = combinedCounts.Where(c => c > 0).ToList();

            DeliveryMinBedsPerMonth = nonZero.Any() ? nonZero.Min() : 0;
            DeliveryMaxBedsPerMonth = combinedCounts.Any() ? combinedCounts.Max() : 0;
            DeliveryAverageBedsPerMonth = nonZero.Any() ? nonZero.Average() : 0.0;

            // Average per week over months that have data (exclude zeros)
            double totalWeeks = 0.0;
            int totalBedsNonZero = 0;

            if (DeliverySeriesPrevYear != null)
            {
                for (int m = 1; m <= 12; m++)
                {
                    var count = DeliverySeriesPrevYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        totalWeeks += DateTime.DaysInMonth(prevYear, m) / 7.0;
                        totalBedsNonZero += count;
                    }
                }
            }
            if (DeliverySeriesCurrentYear != null)
            {
                for (int m = 1; m <= currentMonth; m++)
                {
                    var count = DeliverySeriesCurrentYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        totalWeeks += DateTime.DaysInMonth(currentYear, m) / 7.0;
                        totalBedsNonZero += count;
                    }
                }
            }

            DeliveryAverageBedsPerWeek = totalWeeks > 0 ? totalBedsNonZero / totalWeeks : 0.0;
        }



        protected class ChartPoint
        {
            public string Month { get; set; } = string.Empty;
            public int? Count { get; set; }
        }
    }
}









