using System.Data.Common;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BedBrigade.Data.Services;

public class SmsQueueDataService : Repository<SmsQueue>, ISmsQueueDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private IUserDataService _userDataService;
    private IVolunteerDataService _volunteerDataService;
    private IBedRequestDataService _bedRequestDataService;
    private IContactUsDataService _contactUsDataService;
    private IConfigurationDataService _configDataService;
    private ISmsState _smsState;
    private ILocationDataService _locationDataService;
    private IScheduleDataService _scheduleDataService;
    private readonly ITimezoneDataService _svcTimeZone;

    public SmsQueueDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService, 
        IUserDataService userDataService, 
        IVolunteerDataService volunteerDataService, 
        IBedRequestDataService bedRequestDataService, 
        IContactUsDataService contactUsDataService, 
        IConfigurationDataService configDataService, 
        ISmsState smsState,
        ITimezoneDataService svcTimeZone, 
        ILocationDataService locationDataService, 
        IScheduleDataService scheduleDataService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _userDataService = userDataService;
        _volunteerDataService = volunteerDataService;
        _bedRequestDataService = bedRequestDataService;
        _contactUsDataService = contactUsDataService;
        _configDataService = configDataService;
        _smsState = smsState;
        _svcTimeZone = svcTimeZone;
        _locationDataService = locationDataService;
        _scheduleDataService = scheduleDataService;
    }

    public async Task<List<SmsQueue>> GetLockedMessages()
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetLockedMessages()");
        List<SmsQueue>? cachedContent = _cachingService.Get<List<SmsQueue>>(cacheKey);

        if (cachedContent != null)
        {
            return cachedContent;
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            var result = await dbSet.Where(o => o.Status == SmsQueueStatus.Locked.ToString()).ToListAsync();
            _cachingService.Set(cacheKey, result);
            return result;
        }
    }

    public async Task ClearSmsQueueLock()
    {
        var lockedMessages = await GetLockedMessages();

        if (lockedMessages.Count == 0)
            return;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            foreach (var lockedMessage in lockedMessages)
            {
                lockedMessage.LockDate = null;
                lockedMessage.Status = SmsQueueStatus.Queued.ToString();
                lockedMessage.UpdateDate = DateTime.UtcNow;
                dbSet.Update(lockedMessage);
            }

            await ctx.SaveChangesAsync();
            _cachingService.ClearByEntityName(GetEntityName());
        }
    }

    public async Task<List<SmsQueue>> GetMessagesToProcess(int maxPerChunk)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetMessagesToProcess({maxPerChunk})");
        List<SmsQueue>? cachedContent = _cachingService.Get<List<SmsQueue>>(cacheKey);

        if (cachedContent != null)
        {
            return cachedContent;
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            var result = await dbSet.Where(o => o.Status == SmsQueueStatus.Queued.ToString()
                                                && DateTime.UtcNow >= o.TargetDate)
                .OrderByDescending(o => o.Priority)
                .ThenBy(o => o.QueueDate)
                .Take(maxPerChunk)
                .ToListAsync();
            _cachingService.Set(cacheKey, result);
            return result;
        }
    }

    public async Task DeleteOldSmsQueue(int daysOld)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            var oldMessages = dbSet.Where(o =>
                o.Status != SmsQueueStatus.Queued.ToString() && o.UpdateDate < DateTime.UtcNow.AddDays(-daysOld));

            var oldMessagesList = await oldMessages.ToListAsync();
            if (oldMessagesList.Count > 0)
            {
                dbSet.RemoveRange(oldMessagesList);
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }
    }

    public async Task LockMessagesToProcess(List<SmsQueue> messagesToProcess)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            bool anyLocked = false;
            foreach (var message in messagesToProcess)
            {
                message.LockDate = DateTime.UtcNow;
                message.Status = SmsQueueStatus.Locked.ToString();
                message.UpdateDate = DateTime.UtcNow;
                dbSet.Update(message);
                anyLocked = true;
            }

            if (anyLocked)
            {
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }
    }

    public async Task<ServiceResponse<List<SmsQueueSummary>>> GetSummaryForLocation(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetSummaryForLocation({locationId})");
        List<SmsQueueSummary>? cachedContent = _cachingService.Get<List<SmsQueueSummary>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<SmsQueueSummary>>($"Found {cachedContent.Count} {GetEntityName()} in cache",
                true,
                cachedContent);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var result = await ctx.Set<SmsQueue>()
                    .Where(o => o.LocationId == locationId
                        && (o.Status == SmsQueueStatus.Sent.ToString() 
                        || o.Status == SmsQueueStatus.Received.ToString()))
                    .GroupBy(o => o.ToPhoneNumber)
                    .Select(g => new SmsQueueSummary
                    {
                        ToPhoneNumber = g.Key,
                        MessageCount = g.Count(),
                        UnReadCount = g.Count(o => !o.IsRead),
                        ContactType = g.First().ContactType,
                        ContactName = g.First().ContactName,
                        Body = g.OrderByDescending(m => m.SentDate ?? DateTime.MinValue).First().Body,
                        IsReply =g.OrderByDescending(m => m.SentDate ?? DateTime.MinValue).First().IsReply,
                        MessageDate = g.OrderByDescending(m => m.SentDate ?? DateTime.MinValue).First().SentDate,
                        UnRead = g.Any(o => !o.IsRead)
                    })
                    .OrderByDescending(o => o.MessageDate)
                    .ToListAsync();

                _svcTimeZone.FillLocalDates(result);
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<SmsQueueSummary>>($"Found {result.Count} {GetEntityName()}", true,
                    result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<SmsQueueSummary>>(
                $"Error GetSummaryForLocation for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
        }
    }

    public async Task<ServiceResponse<List<SmsQueue>>> GetMessagesForLocationAndToPhoneNumber(int locationId,
        string toPhoneNumber)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(),
            $"GetMessagesForLocationAndToPhoneNumber({locationId},{toPhoneNumber})");
        List<SmsQueue>? cachedContent = _cachingService.Get<List<SmsQueue>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<SmsQueue>>($"Found {cachedContent.Count} {GetEntityName()} in cache", true,
                cachedContent);
        }

        try
        {


            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<SmsQueue>();
                var result = await dbSet.Where(o => o.LocationId == locationId
                                                    && o.ToPhoneNumber == toPhoneNumber
                                                    && (o.Status == SmsQueueStatus.Sent.ToString() 
                                                    || o.Status == SmsQueueStatus.Received.ToString()))
                    .OrderBy(o => o.SentDate).ToListAsync();

                _svcTimeZone.FillLocalDates(result);
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<SmsQueue>>($"Found {result.Count} {GetEntityName()}", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<SmsQueue>>(
                $"Error GetMessagesForLocationAndToPhoneNumber for {GetEntityName()} : {ex.Message} ({ex.ErrorCode})",
                false, null);
        }
    }




    //This is overridden because we don't want to log the SmsQueue entity
    public override async Task<ServiceResponse<SmsQueue>> CreateAsync(SmsQueue entity)
    {
        string userName = await GetUserName();
        entity.SetCreateAndUpdateUser(userName);

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<SmsQueue>();
                await dbSet.AddAsync(entity);
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
                return new ServiceResponse<SmsQueue>($"Created {GetEntityName()} with id {entity}", true, entity);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<SmsQueue>($"Could not create {GetEntityName()}: {ex.Message} ({ex.ErrorCode})",
                false);
        }
    }

    public override async Task<ServiceResponse<SmsQueue>> UpdateAsync(SmsQueue entity)
    {

        string userName = await GetUserName();
        entity.SetUpdateUser(userName);

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<SmsQueue>();
                dbSet.Update(entity);
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
                return new ServiceResponse<SmsQueue>($"Updated {GetEntityName()} with id {entity}", true, entity);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<SmsQueue>($"Could not update {GetEntityName()}: {ex.Message} ({ex.ErrorCode})",
                false);
        }
    }

    public async Task<ServiceResponse<SmsQueue>> CreateSmsReply(string fromPhoneNumber, string toPhoneNumber,
        string body)
    {
        SmsQueue smsQueue = new SmsQueue
        {
            //This is a reply so we switch the from and to phone numbers
            FromPhoneNumber = toPhoneNumber.FormatPhoneNumber(),
            ToPhoneNumber = fromPhoneNumber.FormatPhoneNumber(),
            Body = body,
            QueueDate = DateTime.UtcNow,
            TargetDate = DateTime.UtcNow,
            SentDate = DateTime.UtcNow,
            Priority = Defaults.BulkHighPriority,
            FailureMessage = string.Empty,
            IsReply = true,
            Status = SmsQueueStatus.Received.ToString(),
            IsRead = false
        };

        string errorMessage = await FillLocationByFromPhoneNumber(smsQueue);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Log.Error(errorMessage);
            return new ServiceResponse<SmsQueue>(errorMessage);
        }

        await FillContactByToPhoneNumber(smsQueue);
        var result = await CreateAsync(smsQueue);

        if (result.Success && result.Data != null)
        {
            await _smsState.NotifyStateChangedAsync(result.Data);
        }

        return result;
    }

    public async Task FillContactByToPhoneNumber(SmsQueue smsQueue)
    {
        if (await FillContactByPhoneNumberFromUser(smsQueue))
            return;

        if (await FillContactByPhoneNumberFromVolunteer(smsQueue))
            return;

        if (await FillContactByPhoneNumberFromBedRequest(smsQueue))
            return;

        if (await FillContactByPhoneNumberFromContactUs(smsQueue))
            return;

        Log.Warning($"FillContactByToPhoneNumber could not find a contact for {smsQueue.ToPhoneNumber}");
        smsQueue.ContactType = ContactTypes.Unknown;
        smsQueue.ContactName = "Unknown";
    }

    private async Task<bool> FillContactByPhoneNumberFromContactUs(SmsQueue smsQueue)
    {
        var result = await _contactUsDataService.GetByPhone(smsQueue.ToPhoneNumber);
        if (result.Success && result.Data != null)
        {
            smsQueue.ContactType = ContactTypes.ContactUs;
            smsQueue.ContactName = result.Data.FullName;
            return true;
        }

        return false;
    }

    private async Task<bool> FillContactByPhoneNumberFromBedRequest(SmsQueue smsQueue)
    {
        var result = await _bedRequestDataService.GetByPhone(smsQueue.ToPhoneNumber);
        if (result.Success && result.Data != null)
        {
            smsQueue.ContactType = ContactTypes.BedRequestor;
            smsQueue.ContactName = result.Data.FullName;
            return true;
        }

        return false;
    }

    private async Task<bool> FillContactByPhoneNumberFromVolunteer(SmsQueue smsQueue)
    {
        var result = await _volunteerDataService.GetByPhone(smsQueue.ToPhoneNumber);
        if (result.Success && result.Data != null)
        {
            smsQueue.ContactType = ContactTypes.Volunteer;
            smsQueue.ContactName = result.Data.FullName;
            return true;
        }

        return false;
    }

    private async Task<bool> FillContactByPhoneNumberFromUser(SmsQueue smsQueue)
    {
        var result = await _userDataService.GetByPhone(smsQueue.ToPhoneNumber);

        if (result.Success && result.Data != null)
        {
            smsQueue.ContactType = ContactTypes.User;
            smsQueue.ContactName = result.Data.FullName;
            return true;
        }

        return false;
    }

    private async Task<string> FillLocationByFromPhoneNumber(SmsQueue smsQueue)
    {
        var result = await _configDataService.GetAllAsync(ConfigSection.Sms);
        if (!result.Success || result.Data == null || !result.Data.Any())
        {
            return "FillLocationByFromPhoneNumber could not get any ConfigSection.Sms";
        }

        var config = result.Data.FirstOrDefault(c => c.ConfigurationKey == ConfigNames.SmsPhone 
                                                     && StringUtil.ExtractDigits(c.ConfigurationValue) == StringUtil.ExtractDigits(smsQueue.FromPhoneNumber));

        if (config == null)
        {
            return $"FillLocationByFromPhoneNumber could not get ConfigNames.SmsPhone for {smsQueue.FromPhoneNumber}";
        }

        smsQueue.LocationId = config.LocationId;
        return string.Empty;
    }

    public async Task MarkMessagesAsRead(int locationId, string toPhoneNumber)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            var messages = await dbSet.Where(o => o.LocationId == locationId 
                                                  && (o.Status == SmsQueueStatus.Sent.ToString()
                                                    || o.Status == SmsQueueStatus.Received.ToString())
                                                  && o.ToPhoneNumber == toPhoneNumber 
                                                  && !o.IsRead).ToListAsync();

            if (messages.Any())
            {
                foreach (var message in messages)
                {
                    message.IsRead = true;
                    dbSet.Update(message);
                }

                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }
    }

    public async Task<ServiceResponse<List<string>>> GetPhoneNumbersToSend(int locationId, SmsRecipientOption option, int scheduleId)
    {
        const string message = "Built Phone List";
        switch (option)
        {
            case SmsRecipientOption.Myself:
                return new ServiceResponse<List<string>>(message, true, (await GetMyself()));
            case SmsRecipientOption.Everyone:
                return new ServiceResponse<List<string>>(message, true, (await GetEveryone()));
            case SmsRecipientOption.VolunteersForLocation:
                return new ServiceResponse<List<string>>(message, true, (await _volunteerDataService.GetDistinctPhoneByLocation(locationId)).Data);
            case SmsRecipientOption.BedRequestorsForLocation:
                return new ServiceResponse<List<string>>(message, true, (await _bedRequestDataService.GetDistinctPhoneByLocation(locationId)).Data);
            case SmsRecipientOption.ContactUsForLocation:
                return new ServiceResponse<List<string>>(message, true, (await _contactUsDataService.GetDistinctPhoneByLocation(locationId)).Data);
            case SmsRecipientOption.BedBrigadeLeadersNationwide:
                return new ServiceResponse<List<string>>(message, true, (await _userDataService.GetDistinctPhone()).Data);
            case SmsRecipientOption.BedBrigadeLeadersForLocation:
                return new ServiceResponse<List<string>>(message, true, (await _userDataService.GetDistinctPhoneByLocation(locationId)).Data);
            case SmsRecipientOption.VolunteersWithDeliveryVehicles:
                return new ServiceResponse<List<string>>(message, true, (await _volunteerDataService.GetVolunteerPhonesWithDeliveryVehicles(locationId)).Data);
            case SmsRecipientOption.VolunteersForAnEvent:
                return new ServiceResponse<List<string>>(message, true, (await _volunteerDataService.GetVolunteerPhonesForASchedule(scheduleId)).Data);
            case SmsRecipientOption.BedRequestorsWhoHaveNotRecievedABed:
                return new ServiceResponse<List<string>>(message, true, (await _bedRequestDataService.PhonesForNotReceivedABed(locationId)).Data);
            case SmsRecipientOption.BedRequestorsWhoHaveRecievedABed:
                return new ServiceResponse<List<string>>(message, true, (await _bedRequestDataService.PhonesForReceivedABed(locationId)).Data);
            case SmsRecipientOption.BedRequestorsForAnEvent:
                return new ServiceResponse<List<string>>(message, true, (await _bedRequestDataService.PhonesForSchedule(locationId)).Data);
            default:
                throw new ArgumentOutOfRangeException(nameof(option), option, $"Unsupported Option: {option}");
        }
    }

    private async Task<List<string>> GetMyself()
    {
        var phone = await GetUserPhone();
        return new List<string> { phone };
    }

    private async Task<List<string>> GetEveryone()
    {
        var volunteers = await _volunteerDataService.GetDistinctPhone();
        var bedRequestors = await _bedRequestDataService.GetDistinctPhone();
        var contactUs = await _contactUsDataService.GetDistinctPhone();
        var users = await _userDataService.GetDistinctPhone();

        var everyone = new List<string>();
        everyone.AddRange(volunteers.Data);
        everyone.AddRange(bedRequestors.Data);
        everyone.AddRange(contactUs.Data);
        everyone.AddRange(users.Data);
        return everyone.Distinct().ToList();
    }

    public async Task<ServiceResponse<string>> QueueBulkSms(int locationId, List<string> phoneNumberList, string body)
    {
        if (phoneNumberList.Count == 0)
            return new ServiceResponse<string>("No text messages to send", false);

        string fromPhoneNumber = await _configDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsPhone, locationId);
        fromPhoneNumber = fromPhoneNumber.FormatPhoneNumber();

        try
        {
            var smsQueueList = phoneNumberList.Select(phone => new SmsQueue()
            {
                FromPhoneNumber = fromPhoneNumber,
                ToPhoneNumber = phone.FormatPhoneNumber(),
                Body = body,
                Priority = Defaults.BulkLowPriority,
                Status = SmsQueueStatus.Queued.ToString(),
                QueueDate = DateTime.UtcNow,
                FailureMessage = string.Empty,
                TargetDate = DateTime.UtcNow,
                IsRead = true,
                IsReply = false,
                LocationId = locationId
            }).ToList();

            string userName = await GetUserName();

            foreach (var smsQueue in smsQueueList)
            {
                smsQueue.SetCreateAndUpdateUser(userName);
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<SmsQueue>();
                await dbSet.AddRangeAsync(smsQueueList);
                await ctx.SaveChangesAsync();
            }

            _cachingService.ClearByEntityName(GetEntityName());

            return new ServiceResponse<string>($"Queued {smsQueueList.Count} text messages", true, SmsQueueStatus.Queued.ToString());
        }
        catch (Exception ex)
        {
            return new ServiceResponse<string>($"Failed to queue bulk text messages: {ex.Message}", false);
        }
    }

    public async Task<ServiceResponse<string>> GetSendPlanMessage(int locationId, SmsRecipientOption option, int scheduleId)
    {
        try
        {
            int queueCount = await GetQueueCount();
            int phoneNumberCount = (await GetPhoneNumbersToSend(locationId, option, scheduleId)).Data.Count;
            string estimatedTime = await GetEstimatedTime(queueCount, phoneNumberCount);
            string liveTextMessage = await IsLiveSms() ? "Text Messaging is LIVE." : "Text Messaging is NOT live, it is logged.";
            string locationName = await GetLocationName(locationId);
            string eventName = await GetEventName(scheduleId);
            string message;
            if (option == SmsRecipientOption.Everyone || option == SmsRecipientOption.BedBrigadeLeadersNationwide)
            {
                message = $"{phoneNumberCount} text messages will be sent to {EnumHelper.GetEnumDescription(option)}. There are currently {queueCount} other text messages in the queue. It will take an estimated {estimatedTime} to send. {liveTextMessage}";
            }
            else if (option.ToString().Contains("Event"))
            {
                message = $"{phoneNumberCount} text messages will be sent to {EnumHelper.GetEnumDescription(option)} {locationName} for the event {eventName}. There are currently {queueCount} other text messages in the queue. It will take an estimated {estimatedTime} to send. {liveTextMessage}";
            }
            else
            {
                message = $"{phoneNumberCount} text messages will be sent to {EnumHelper.GetEnumDescription(option)} {locationName}. There are currently {queueCount} other text messages in the queue. It will take an estimated {estimatedTime} to send. {liveTextMessage}";
            }

            return new ServiceResponse<string>("Created plan message", true, message);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<string>($"Could not GetSendPlanMessage {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
        }
    }

    public async Task<int> GetQueueCount()
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetQueueCount()");
        int? cachedContent = _cachingService.Get<int>(cacheKey);

        if (cachedContent.HasValue)
        {
            return cachedContent.Value;
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            var result = await dbSet.CountAsync(o => o.Status == SmsQueueStatus.Queued.ToString());
            _cachingService.Set(cacheKey, result);
            return result;
        }
    }

    public async Task<string> GetEstimatedTime(int queueCount, int phoneNumberCount)
    {
        int totalCount = queueCount + phoneNumberCount;
        int maxTextMessagesPerSecond = Convert.ToInt32((await _configDataService.GetByIdAsync(ConfigNames.SmsMaxSendPerSecond)).Data.ConfigurationValue);

        if (totalCount <= maxTextMessagesPerSecond)
        {
            return "1 minute";
        }


        double seconds = Convert.ToDouble(totalCount) / Convert.ToDouble(maxTextMessagesPerSecond);
        seconds += 60;

        long milliseconds = Convert.ToInt64(seconds * (double)1000);
        return DateUtil.MillisecondsToTimeLapse(milliseconds);
    }

    private async Task<bool> IsLiveSms()
    {
        string configValue = (await _configDataService.GetByIdAsync(ConfigNames.SmsUseFileMock)).Data.ConfigurationValue;
        return configValue != "true";
    }

    private async Task<string> GetEventName(int scheduleId)
    {
        if (scheduleId == 0)
        {
            return string.Empty;
        }

        return (await _scheduleDataService.GetByIdAsync(scheduleId)).Data.EventSelect;
    }

    private async Task<string> GetLocationName(int locationId)
    {
        return (await _locationDataService.GetByIdAsync(locationId)).Data.Name;
    }

    public async Task<ServiceResponse<List<SmsQueue>>> GetOldUnreadMessages()
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetOldUnreadMessages()");

        List<SmsQueue>? cachedContent = _cachingService.Get<List<SmsQueue>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<SmsQueue>>($"Found {cachedContent.Count} {GetEntityName()} in cache", true,
                cachedContent);
        }

        int missedMessageMinutes = await _configDataService.GetConfigValueAsIntAsync(ConfigSection.Sms, ConfigNames.SmsMissedMessageMinutes);

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<SmsQueue>();
                var result = await dbSet.Where(o => o.IsRead == false && o.SentDate < DateTime.UtcNow.AddMinutes(missedMessageMinutes * -1))
                    .OrderBy(o => o.SentDate)
                    .ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<SmsQueue>>($"Found {result.Count} unread messages older than 30 minutes", true, result);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<SmsQueue>>(
                $"Error GetOldUnreadMessages for {GetEntityName()} : {ex.Message} ({ex.ErrorCode})",
                false, null);
        }
    }
}



