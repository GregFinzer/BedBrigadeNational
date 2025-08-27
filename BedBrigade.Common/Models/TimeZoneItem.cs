namespace BedBrigade.Common.Models;

public class TimeZoneItem
{
    public string TimeZoneId { get; set; }
    public string TimeZoneWithGmt { get; set; }

    public TimeZoneItem(TimeZoneInfo timeZone)
    {
        TimeZoneId = timeZone.Id;
        TimeSpan offset = timeZone.BaseUtcOffset;
        string sign = offset.TotalHours >= 0 ? "+" : "-";
        TimeZoneWithGmt = $"{timeZone.Id} (GMT {sign}{Math.Abs(offset.TotalHours)})";
    }
}

