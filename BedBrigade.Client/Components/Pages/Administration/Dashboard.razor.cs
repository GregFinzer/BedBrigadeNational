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
        // New: monthly request counts for Bed Requests
        protected List<ChartPoint>? BedRequestRequestsSeriesCurrentYear { get; set; }
        protected List<ChartPoint>? BedRequestRequestsSeriesPrevYear { get; set; }
        protected List<ChartPoint>? BedRequestRequestsSeriesTwoYearsAgo { get; set; }
        // Chart data for Bed Delivery History
        protected bool IsDeliveryHistoryLoading { get; set; } = true;
        protected List<ChartPoint>? DeliverySeriesCurrentYear { get; set; }
        protected List<ChartPoint>? DeliverySeriesPrevYear { get; set; }
        protected List<ChartPoint>? DeliverySeriesTwoYearsAgo { get; set; }
        // New: monthly request counts for Deliveries (number of delivered requests)
        protected List<ChartPoint>? DeliveryRequestsSeriesCurrentYear { get; set; }
        protected List<ChartPoint>? DeliveryRequestsSeriesPrevYear { get; set; }
        protected List<ChartPoint>? DeliveryRequestsSeriesTwoYearsAgo { get; set; }
        protected string LocationName { get; set; }

        // Statistics for Bed Requests (Beds totals)
        protected int CurrentYearYtdTotal { get; set; }
        protected int PrevYearTotal { get; set; }
        protected int TwoYearsAgoTotal { get; set; }
        protected int MinBedsPerMonth { get; set; }
        protected int MaxBedsPerMonth { get; set; }
        protected double AverageBedsPerMonth { get; set; }
        protected double AverageBedsPerWeek { get; set; }
        // New: Statistics for Bed Requests (Request counts)
        protected int CurrentYearYtdRequests { get; set; }
        protected int PrevYearRequestsTotal { get; set; }
        protected int TwoYearsAgoRequestsTotal { get; set; }
        protected int MinRequestsPerMonth { get; set; }
        protected int MaxRequestsPerMonth { get; set; }
        protected double AverageRequestsPerMonth { get; set; }
        protected double AverageRequestsPerWeek { get; set; }

        // Statistics for Bed Deliveries (Beds totals)
        protected int DeliveryCurrentYearYtdBeds { get; set; }
        protected int DeliveryPrevYearBeds { get; set; }
        protected int DeliveryTwoYearsAgoBeds { get; set; }
        protected int DeliveryMinBedsPerMonth { get; set; }
        protected int DeliveryMaxBedsPerMonth { get; set; }
        protected double DeliveryAverageBedsPerMonth { get; set; }
        protected double DeliveryAverageBedsPerWeek { get; set; }
        // Statistics for Bed Deliveries (Request counts)
        protected int DeliveryCurrentYearYtdRequests { get; set; }
        protected int DeliveryPrevYearRequestsTotal { get; set; }
        protected int DeliveryTwoYearsAgoRequestsTotal { get; set; }
        protected int DeliveryMinRequestsPerMonth { get; set; }
        protected int DeliveryMaxRequestsPerMonth { get; set; }
        protected double DeliveryAverageRequestsPerMonth { get; set; }
        protected double DeliveryAverageRequestsPerWeek { get; set; }

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

                // Build request-count series
                BedRequestRequestsSeriesCurrentYear = BuildSeries(data, currentYear, r => r.Requests);
                BedRequestRequestsSeriesPrevYear = BuildSeries(data, currentYear - 1, r => r.Requests);
                BedRequestRequestsSeriesTwoYearsAgo = BuildSeries(data, currentYear - 2, r => r.Requests);

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

                // Build request-count series for deliveries
                DeliveryRequestsSeriesCurrentYear = BuildSeries(data, currentYear, r => r.Requests);
                DeliveryRequestsSeriesPrevYear = BuildSeries(data, currentYear - 1, r => r.Requests);
                DeliveryRequestsSeriesTwoYearsAgo = BuildSeries(data, currentYear - 2, r => r.Requests);

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
            return BuildSeries(rows, year, r => r.Beds);
        }

        private static List<ChartPoint> BuildSeries(List<BedRequestHistoryRow> rows, int year, Func<BedRequestHistoryRow, int> selector)
        {
            var months = Enumerable.Range(1, 12);
            var dict = rows.Where(r => r.Year == year).ToDictionary(r => r.Month, selector);
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

            // Beds-based statistics (unchanged)
            var ytdBeds = BedRequestSeriesCurrentYear?.Take(currentMonth).Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var prevYearBeds = BedRequestSeriesPrevYear?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var twoYearsAgoBeds = BedRequestSeriesTwoYearsAgo?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();

            CurrentYearYtdTotal = ytdBeds.Sum();
            PrevYearTotal = prevYearBeds.Sum();
            TwoYearsAgoTotal = twoYearsAgoBeds.Sum();

            var bedsCombined = prevYearBeds.Concat(ytdBeds).ToList();
            var bedsNonZero = bedsCombined.Where(c => c > 0).ToList();

            MinBedsPerMonth = bedsNonZero.Any() ? bedsNonZero.Min() : 0;
            MaxBedsPerMonth = bedsCombined.Any() ? bedsCombined.Max() : 0;
            AverageBedsPerMonth = bedsNonZero.Any() ? bedsNonZero.Average() : 0.0;

            double bedsWeeks = 0.0;
            int bedsTotalNonZero = 0;
            if (BedRequestSeriesPrevYear != null)
            {
                for (int m = 1; m <= 12; m++)
                {
                    var count = BedRequestSeriesPrevYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        bedsWeeks += DateTime.DaysInMonth(prevYear, m) / 7.0;
                        bedsTotalNonZero += count;
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
                        bedsWeeks += DateTime.DaysInMonth(currentYear, m) / 7.0;
                        bedsTotalNonZero += count;
                    }
                }
            }
            AverageBedsPerWeek = bedsWeeks > 0 ? bedsTotalNonZero / bedsWeeks : 0.0;

            // Requests-based statistics
            var ytdReq = BedRequestRequestsSeriesCurrentYear?.Take(currentMonth).Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var prevYearReq = BedRequestRequestsSeriesPrevYear?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var twoYearsAgoReq = BedRequestRequestsSeriesTwoYearsAgo?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();

            CurrentYearYtdRequests = ytdReq.Sum();
            PrevYearRequestsTotal = prevYearReq.Sum();
            TwoYearsAgoRequestsTotal = twoYearsAgoReq.Sum();

            var reqCombined = prevYearReq.Concat(ytdReq).ToList();
            var reqNonZero = reqCombined.Where(c => c > 0).ToList();
            MinRequestsPerMonth = reqNonZero.Any() ? reqNonZero.Min() : 0;
            MaxRequestsPerMonth = reqCombined.Any() ? reqCombined.Max() : 0;
            AverageRequestsPerMonth = reqNonZero.Any() ? reqNonZero.Average() : 0.0;

            double reqWeeks = 0.0;
            int reqTotalNonZero = 0;
            if (BedRequestRequestsSeriesPrevYear != null)
            {
                for (int m = 1; m <= 12; m++)
                {
                    var count = BedRequestRequestsSeriesPrevYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        reqWeeks += DateTime.DaysInMonth(prevYear, m) / 7.0;
                        reqTotalNonZero += count;
                    }
                }
            }
            if (BedRequestRequestsSeriesCurrentYear != null)
            {
                for (int m = 1; m <= currentMonth; m++)
                {
                    var count = BedRequestRequestsSeriesCurrentYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        reqWeeks += DateTime.DaysInMonth(currentYear, m) / 7.0;
                        reqTotalNonZero += count;
                    }
                }
            }
            AverageRequestsPerWeek = reqWeeks > 0 ? reqTotalNonZero / reqWeeks : 0.0;
        }

        private void ComputeDeliveryStatistics()
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var prevYear = currentYear - 1;

            // Beds-based statistics
            var ytdBeds = DeliverySeriesCurrentYear?.Take(currentMonth).Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var prevYearBeds = DeliverySeriesPrevYear?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var twoYearsAgoBeds = DeliverySeriesTwoYearsAgo?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();

            DeliveryCurrentYearYtdBeds = ytdBeds.Sum();
            DeliveryPrevYearBeds = prevYearBeds.Sum();
            DeliveryTwoYearsAgoBeds = twoYearsAgoBeds.Sum();

            var bedsCombined = prevYearBeds.Concat(ytdBeds).ToList();
            var bedsNonZero = bedsCombined.Where(c => c > 0).ToList();

            DeliveryMinBedsPerMonth = bedsNonZero.Any() ? bedsNonZero.Min() : 0;
            DeliveryMaxBedsPerMonth = bedsCombined.Any() ? bedsCombined.Max() : 0;
            DeliveryAverageBedsPerMonth = bedsNonZero.Any() ? bedsNonZero.Average() : 0.0;

            double bedsWeeks = 0.0;
            int bedsTotalNonZero = 0;
            if (DeliverySeriesPrevYear != null)
            {
                for (int m = 1; m <= 12; m++)
                {
                    var count = DeliverySeriesPrevYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        bedsWeeks += DateTime.DaysInMonth(prevYear, m) / 7.0;
                        bedsTotalNonZero += count;
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
                        bedsWeeks += DateTime.DaysInMonth(currentYear, m) / 7.0;
                        bedsTotalNonZero += count;
                    }
                }
            }

            DeliveryAverageBedsPerWeek = bedsWeeks > 0 ? bedsTotalNonZero / bedsWeeks : 0.0;

            // Requests-based statistics
            var ytdReq = DeliveryRequestsSeriesCurrentYear?.Take(currentMonth).Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var prevYearReq = DeliveryRequestsSeriesPrevYear?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();
            var twoYearsAgoReq = DeliveryRequestsSeriesTwoYearsAgo?.Select(p => p.Count ?? 0) ?? Enumerable.Empty<int>();

            DeliveryCurrentYearYtdRequests = ytdReq.Sum();
            DeliveryPrevYearRequestsTotal = prevYearReq.Sum();
            DeliveryTwoYearsAgoRequestsTotal = twoYearsAgoReq.Sum();

            var reqCombined = prevYearReq.Concat(ytdReq).ToList();
            var reqNonZero = reqCombined.Where(c => c > 0).ToList();
            DeliveryMinRequestsPerMonth = reqNonZero.Any() ? reqNonZero.Min() : 0;
            DeliveryMaxRequestsPerMonth = reqCombined.Any() ? reqCombined.Max() : 0;
            DeliveryAverageRequestsPerMonth = reqNonZero.Any() ? reqNonZero.Average() : 0.0;

            double reqWeeks = 0.0;
            int reqTotalNonZero = 0;
            if (DeliveryRequestsSeriesPrevYear != null)
            {
                for (int m = 1; m <= 12; m++)
                {
                    var count = DeliveryRequestsSeriesPrevYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        reqWeeks += DateTime.DaysInMonth(prevYear, m) / 7.0;
                        reqTotalNonZero += count;
                    }
                }
            }
            if (DeliveryRequestsSeriesCurrentYear != null)
            {
                for (int m = 1; m <= currentMonth; m++)
                {
                    var count = DeliveryRequestsSeriesCurrentYear[m - 1].Count ?? 0;
                    if (count > 0)
                    {
                        reqWeeks += DateTime.DaysInMonth(currentYear, m) / 7.0;
                        reqTotalNonZero += count;
                    }
                }
            }

            DeliveryAverageRequestsPerWeek = reqWeeks > 0 ? reqTotalNonZero / reqWeeks : 0.0;
        }



        protected class ChartPoint
        {
            public string Month { get; set; } = string.Empty;
            public int? Count { get; set; }
        }
    }
}









