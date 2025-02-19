﻿using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services;

public interface ITimezoneDataService
{
    string GetUserTimeZoneId();
    List<TimeZoneItem> GetTimeZones();
    DateTime? ConvertUtcToTimeZone(DateTime? utcDate, string timeZoneId);
    void FillLocalDates<T>(List<T> items) where T : BaseEntity;
    void FillLocalDates(List<SmsQueue> items);
    void FillLocalDates(List<SmsQueueSummary> items);
}
