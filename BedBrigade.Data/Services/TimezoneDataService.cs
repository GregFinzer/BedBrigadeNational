using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services;

public class TimezoneDataService : ITimezoneDataService
{
    public IAuthService _authService;
    private string _userTimeZoneId;

    public TimezoneDataService(IAuthService authService)
    {
        _authService = authService;
    }

    public string GetUserTimeZoneId()
    {
        if (string.IsNullOrWhiteSpace(_userTimeZoneId))
        {
            _userTimeZoneId = _authService.TimeZoneId;
        }
        return _userTimeZoneId;
    }

    public List<TimeZoneItem> GetTimeZones()
    {
        return TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new TimeZoneItem(tz))
            .ToList();
    }

    public DateTime? ConvertUtcToTimeZone(DateTime? utcDate, string timeZoneId)
    {
        if (utcDate == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            timeZoneId = Defaults.DefaultTimeZoneId;
        }

        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDate.Value, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid TimeZoneId: {timeZoneId}");
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException($"Corrupt TimeZone data: {timeZoneId}");
        }
    }

    public void FillLocalDates<T>(List<T> items) where T : BaseEntity
    {
        string timeZoneId = GetUserTimeZoneId();

        foreach (var item in items)
        {
            item.CreateDateLocal = ConvertUtcToTimeZone(item.CreateDate, timeZoneId);
            item.UpdateDateLocal = ConvertUtcToTimeZone(item.UpdateDate, timeZoneId);
        }
    }

    public void FillLocalDates(List<SmsQueue> items)
    {
        // Updated to fill all local date properties for SmsQueue
        string timeZoneId = GetUserTimeZoneId();
        foreach (var item in items)
        {
            item.QueueDateLocal = ConvertUtcToTimeZone(item.QueueDate, timeZoneId) ?? item.QueueDate;
            item.TargetDateLocal = ConvertUtcToTimeZone(item.TargetDate, timeZoneId) ?? item.TargetDate;
            item.SentDateLocal = ConvertUtcToTimeZone(item.SentDate, timeZoneId);
        }
    }

    public void FillLocalDates(List<SmsQueueSummary> items)
    {
        string timeZoneId = GetUserTimeZoneId();
        foreach (var item in items)
        {
            item.MessageDate = ConvertUtcToTimeZone(item.MessageDate, timeZoneId);
        }
    }

    public void FillLocalDates<T>(T item) where T : BaseEntity
    {
        string timeZoneId = GetUserTimeZoneId();
        item.CreateDateLocal = ConvertUtcToTimeZone(item.CreateDate, timeZoneId);
        item.UpdateDateLocal = ConvertUtcToTimeZone(item.UpdateDate, timeZoneId);
    }

    public void FillLocalDates(List<EmailQueue> items)
    {
        string timeZoneId = GetUserTimeZoneId();
        foreach (var item in items)
        {
            item.CreateDateLocal = ConvertUtcToTimeZone(item.CreateDate, timeZoneId);
            item.UpdateDateLocal = ConvertUtcToTimeZone(item.UpdateDate, timeZoneId);
            item.SentDateLocal = ConvertUtcToTimeZone(item.SentDate, timeZoneId);
            item.QueueDateLocal = ConvertUtcToTimeZone(item.QueueDate, timeZoneId) ?? item.QueueDate;
        }
    }

    public void FillLocalDates(List<TranslationQueueView> items)
    {
        string timeZoneId = GetUserTimeZoneId();
        foreach (var item in items)
        {
            item.QueueDateLocal = ConvertUtcToTimeZone(item.QueueDate, timeZoneId) ?? item.QueueDate;
            item.SentDateLocal = ConvertUtcToTimeZone(item.SentDate, timeZoneId);
            item.LockDateLocal = ConvertUtcToTimeZone(item.LockDate, timeZoneId);
        }
    }
}




