﻿using BedBrigade.Common;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services
{
    public class EmailQueueDataService : Repository<EmailQueue>, IEmailQueueDataService
    {
        
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;
        
        public EmailQueueDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
        }

        public async Task<List<EmailQueue>> GetLockedEmails()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetLockedEmails()");
            List<EmailQueue>? cachedContent = _cachingService.Get<List<EmailQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<EmailQueue>();
                var result = await dbSet.Where(o => o.Status == EmailQueueStatus.Locked.ToString()).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task<List<EmailQueue>> GetEmailsSentToday()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetEmailsSentToday()");
            List<EmailQueue>? cachedContent = _cachingService.Get<List<EmailQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<EmailQueue>();
                var result = await dbSet.Where(o => o.SentDate.HasValue && o.SentDate.Value.Date == DateTime.UtcNow.Date).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task ClearEmailQueueLock()
        {
            var lockedEmails = await GetLockedEmails();

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<EmailQueue>();
                foreach (var lockedEmail in lockedEmails)
                {
                    lockedEmail.LockDate = null;
                    lockedEmail.Status = EmailQueueStatus.Queued.ToString();
                    lockedEmail.UpdateDate = DateTime.UtcNow;
                    dbSet.Update(lockedEmail);
                }
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }   
        }

        public async Task<List<EmailQueue>> GetEmailsToProcess(int maxPerChunk)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetEmailsToProcess({maxPerChunk})");
            List<EmailQueue>? cachedContent = _cachingService.Get<List<EmailQueue>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<EmailQueue>();
                var result = await dbSet.Where(o => o.Status == EmailQueueStatus.Queued.ToString())
                    .OrderByDescending(o => o.Priority)
                    .ThenBy(o => o.QueueDate)
                    .Take(maxPerChunk)
                    .ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }


        public async Task DeleteOldEmailQueue(int daysOld)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<EmailQueue>();
                var oldEmails = dbSet.Where(o =>
                    o.Status != EmailQueueStatus.Queued.ToString() && o.UpdateDate < DateTime.UtcNow.AddDays(-daysOld));
                dbSet.RemoveRange(oldEmails);
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }

        public async Task LockEmailsToProcess(List<EmailQueue> emailsToProcess)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<EmailQueue>();
                foreach (var email in emailsToProcess)
                {
                    email.LockDate = DateTime.UtcNow;
                    email.Status = EmailQueueStatus.Locked.ToString();
                    email.UpdateDate = DateTime.UtcNow;
                    dbSet.Update(email);
                }
                await ctx.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
            }
        }

        //This is overridden because we don't want to log the EmailQueue entity
        public override async Task<ServiceResponse<EmailQueue>> CreateAsync(EmailQueue entity)
        {
            string userName = await GetUserName();
            entity.SetCreateAndUpdateUser(userName);
            
            try
            {
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<EmailQueue>();
                    await dbSet.AddAsync(entity);
                    await ctx.SaveChangesAsync();
                    _cachingService.ClearByEntityName(GetEntityName());
                    return new ServiceResponse<EmailQueue>($"Created {GetEntityName()} with id {entity}", true, entity);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<EmailQueue>($"Could not create {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
        }

        public override async Task<ServiceResponse<EmailQueue>> UpdateAsync(EmailQueue entity)
        {

            string userName = await GetUserName();
            entity.SetUpdateUser(userName);

            try
            {
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<EmailQueue>();
                    dbSet.Update(entity);
                    await ctx.SaveChangesAsync();
                    _cachingService.ClearByEntityName(GetEntityName());
                    return new ServiceResponse<EmailQueue>($"Updated {GetEntityName()} with id {entity}", true, entity);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<EmailQueue>($"Could not update {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
        }
    }
}
