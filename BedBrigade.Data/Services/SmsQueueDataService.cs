using System.Data.Common;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

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
            dbSet.RemoveRange(oldMessages);
            await ctx.SaveChangesAsync();
            _cachingService.ClearByEntityName(GetEntityName());
        }
    }

    public async Task LockMessagesToProcess(List<SmsQueue> messagesToProcess)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<SmsQueue>();
            foreach (var message in messagesToProcess)
            {
                message.LockDate = DateTime.UtcNow;
                message.Status = SmsQueueStatus.Locked.ToString();
                message.UpdateDate = DateTime.UtcNow;
                dbSet.Update(message);
            }

            await ctx.SaveChangesAsync();
            _cachingService.ClearByEntityName(GetEntityName());
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

    //public async Task<ServiceResponse<bool>> DeleteByAppointmentId(int appointmentId)
    //{
    //    try
    //    {
    //        using (var ctx = _contextFactory.CreateDbContext())
    //        {
    //            var dbSet = ctx.Set<SmsQueue>();
    //            var messages = await dbSet.Where(o => o.AppointmentId == appointmentId).ToListAsync();

    //            if (messages.Count == 0)
    //            {
    //                return new ServiceResponse<bool>($"No {GetEntityName()} to delete for appointment {appointmentId}", true, true);
    //            }

    //            dbSet.RemoveRange(messages);
    //            await ctx.SaveChangesAsync();
    //            _cachingService.ClearByEntityName(GetEntityName());
    //            return new ServiceResponse<bool>(
    //                $"Deleted {messages.Count} {GetEntityName()} for appointment {appointmentId}", true, true);
    //        }
    //    }
    //    catch (DbException ex)
    //    {
    //        return new ServiceResponse<bool>(
    //            $"Could not DeleteByAppointmentId {appointmentId} {GetEntityName()}: {ex.Message} ({ex.ErrorCode})",
    //            false);
    //    }
    //}

}



