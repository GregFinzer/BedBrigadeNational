using System.Data.Common;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Twilio.Rest.Studio.V2.Flow;


namespace BedBrigade.Data.Services;

public class SmsQueueDataService : Repository<SmsQueue>, ISmsQueueDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;

    public SmsQueueDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
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
                        UnRead = g.All(o => !o.IsRead)
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




}



