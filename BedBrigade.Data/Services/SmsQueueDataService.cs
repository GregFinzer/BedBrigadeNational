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
    private IUserDataService _svcUser;
    private IVolunteerDataService _svcVolunteer;
    private IBedRequestDataService _svcBedRequest;
    private IContactUsDataService _svcContactUs;
    private IConfigurationDataService _svcConfiguration;
    private ISmsState _smsState;

    public SmsQueueDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService, 
        IUserDataService svcUser, 
        IVolunteerDataService svcVolunteer, 
        IBedRequestDataService svcBedRequest, 
        IContactUsDataService svcContactUs, 
        IConfigurationDataService svcConfiguration, 
        ISmsState smsState) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _svcUser = svcUser;
        _svcVolunteer = svcVolunteer;
        _svcBedRequest = svcBedRequest;
        _svcContactUs = svcContactUs;
        _svcConfiguration = svcConfiguration;
        _smsState = smsState;
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
                                                && DateTime.Now >= o.TargetDate)
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
                    .Where(o => o.LocationId == locationId)
                    .GroupBy(o => o.ToPhoneNumber)
                    .Select(g => new SmsQueueSummary
                    {
                        ToPhoneNumber = g.Key,
                        MessageCount = g.Count(),
                        ContactType = g.First().ContactType,
                        ContactName = g.First().ContactName,
                        Body = g.OrderByDescending(m => m.SentDate ?? DateTime.MinValue).First().Body,
                        MessageDate = g.OrderByDescending(m => m.SentDate ?? DateTime.MinValue).First().SentDate,
                        UnRead = g.Any(o => !o.IsRead)
                    })
                    .ToListAsync();

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
                                                    && o.ToPhoneNumber == toPhoneNumber)
                    .OrderBy(o => o.SentDate).ToListAsync();
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
            Priority = 1,
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
        var result = await _svcContactUs.GetByPhone(smsQueue.ToPhoneNumber);
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
        var result = await _svcBedRequest.GetByPhone(smsQueue.ToPhoneNumber);
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
        var result = await _svcVolunteer.GetByPhone(smsQueue.ToPhoneNumber);
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
        var result = await _svcUser.GetByPhone(smsQueue.ToPhoneNumber);

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
        var result = await _svcConfiguration.GetAllAsync(ConfigSection.Sms);
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
}



