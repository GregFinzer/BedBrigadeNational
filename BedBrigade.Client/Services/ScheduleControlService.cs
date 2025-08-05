using System.Text;
using BedBrigade.Data.Services;
using System.Text.RegularExpressions;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Syncfusion.Drawing;

namespace BedBrigade.Client.Services
{
    public class ScheduleControlService : IScheduleControlService
    {
        private readonly IScheduleDataService _scheduleDataService;
        private readonly ILocationDataService _locationDataService;
        private const string _pattern = @"<div\s+data-component=['""]bbschedule['""]\s+data-months=['""](?<months>\d+)['""]\s+id=['""][a-zA-Z0-9\-]+['""]></div>";
        private static readonly Regex _regex = new Regex(_pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ScheduleControlService(IScheduleDataService scheduleDataService, ILocationDataService locationDataService)
        {
            _scheduleDataService = scheduleDataService;
            _locationDataService = locationDataService;
        }

        public async Task<string> ReplaceScheduleControl(string htmlText, int locationId)
        {
            var tags = GetBbScheduleTags(htmlText);

            if (!tags.Any())
                return htmlText;

            var locationResult = await _locationDataService.GetByIdAsync(locationId);

            if (!locationResult.Success || locationResult.Data == null)
                return htmlText;

            foreach (var tag in tags)
            {
                var scheduleResult = await _scheduleDataService.GetScheduleForMonthsAndLocation(locationId, tag.months);

                if (!scheduleResult.Success || scheduleResult.Data == null)
                    return htmlText;

                htmlText = htmlText.Replace(tag.Tag, GenerateScheduleControlHtml(scheduleResult.Data, locationResult.Data.Route));
            }
            return htmlText;
        }

        private string? GenerateScheduleControlHtml(List<Schedule> schedules, string locationRoute)
        {
            if (schedules == null || schedules.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            // Order schedules by date and group them by the month number.
            var groupedSchedules = schedules.OrderBy(s => s.EventDateScheduled)
                .GroupBy(s => s.EventDateScheduled.Month);

            foreach (var group in groupedSchedules)
            {
                // Get the month abbreviation from the first schedule in the group.
                var firstEvent = group.First();
                string monthAbbrev = firstEvent.EventDateScheduled.ToString("MMM").ToUpperInvariant();

                sb.AppendLine("<div class=\"polaris-home-bipanel-container\">");

                // Left side: display the month abbreviation.
                sb.AppendLine("    <div class=\"polaris-home-bipanel-left\">");
                sb.AppendLine($"        <h3 class=\"polaris-home-bipanel-title\">{monthAbbrev}</h3>");
                sb.AppendLine("    </div>");

                // Right side: list each schedule for the month.
                sb.AppendLine("    <div class=\"polaris-home-bipanel-right\">");
                foreach (var schedule in group)
                {
                    var route = locationRoute.TrimStart('/').TrimEnd('/');
                    string volunteerUrl = $"/{route}/volunteer/{schedule.ScheduleId}";
                    string eventSelect = schedule.EventSelect ?? string.Empty;
                    string address = schedule.Address + ", " +
                        schedule.City + ", " +
                        schedule.State;
                    string mapUrl = GoogleMaps.CreateGoogleMapsLinkForSchedule(schedule);

                    sb.AppendLine("        <p>");
                    sb.AppendLine($"            <a class=\"polaris-a-bold\" href=\"{volunteerUrl}\">{eventSelect}</a>");
                    sb.AppendLine("        </p>");
                    sb.AppendLine("        <p class=\"polaris-home-bipanel-info\">");
                    sb.AppendLine($"            <a class=\"polaris-a\" href=\"{mapUrl}\" target=\"_blank\">{address}</a>");
                    sb.AppendLine("        </p>");
                    sb.AppendLine("<br />");
                }
                sb.AppendLine("    </div>");
                sb.AppendLine("</div>");
            }

            return sb.ToString();
        }


        private List<(string Tag, int months)> GetBbScheduleTags(string htmlText)
        {
            List<(string Tag, int months)> tags = new List<(string Tag, int months)>();
            var matches = _regex.Matches(htmlText);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    tags.Add((match.Value, int.Parse(match.Groups["months"].Value)));
                }
            }

            return tags;
        }
    }
}
