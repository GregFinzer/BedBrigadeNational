using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;

using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Client.Services;

namespace BedBrigade.Data.Services
{
    public class EmailQueueDataService : Repository<EmailQueue>, IEmailQueueDataService
    {
        
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;
        private readonly IVolunteerDataService _volunteerDataService;
        private readonly IBedRequestDataService _bedRequestDataService;
        private readonly IContactUsDataService _contactUsDataService;
        private readonly IUserDataService _userDataService;
        private readonly IConfigurationDataService _configurationDataService;
        private readonly ILocationDataService _locationDataService;
        private readonly IScheduleDataService _scheduleDataService;

        public EmailQueueDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService,
            IAuthService authService,
            IVolunteerDataService volunteerDataService,
            IBedRequestDataService bedRequestDataService,
            IContactUsDataService contactUsDataService,
            IUserDataService userDataService,
            IConfigurationDataService configurationDataService,
            ILocationDataService locationDataService,
            IScheduleDataService scheduleDataService) : base(contextFactory, cachingService, authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
            _volunteerDataService = volunteerDataService;
            _bedRequestDataService = bedRequestDataService;
            _contactUsDataService = contactUsDataService;
            _userDataService = userDataService;
            _configurationDataService = configurationDataService;
            _locationDataService = locationDataService;
            _scheduleDataService = scheduleDataService;
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

        public async Task<ServiceResponse<string>> GetSendPlanMessage(int locationId, EmailRecipientOption option, int scheduleId)
        {
            try
            {
                int queueCount = await GetQueueCount();
                int emailCount = (await GetEmailsToSend(locationId, option, scheduleId)).Data.Count;
                string estimatedTime = await GetEstimatedTime(queueCount, emailCount);
                string liveEmailMessage = await IsLiveEmail() ? "Email is LIVE." : "Email is NOT live, it is logged.";
                string locationName = await GetLocationName(locationId);
                string eventName = await GetEventName(scheduleId);
                string message;
                if (option == EmailRecipientOption.Everyone || option == EmailRecipientOption.BedBrigadeLeadersNationwide)
                {
                    message = $"{emailCount} emails will be sent to {EnumHelper.GetEnumDescription(option)}. There are currently {queueCount} other emails in the queue. It will take an estimated {estimatedTime} to send. {liveEmailMessage}";
                }
                else if (option.ToString().Contains("Event"))
                {
                    message = $"{emailCount} emails will be sent to {EnumHelper.GetEnumDescription(option)} {locationName} for the event {eventName}. There are currently {queueCount} other emails in the queue. It will take an estimated {estimatedTime} to send. {liveEmailMessage}";
                }
                else
                {
                    message = $"{emailCount} emails will be sent to {EnumHelper.GetEnumDescription(option)} {locationName}. There are currently {queueCount} other emails in the queue. It will take an estimated {estimatedTime} to send. {liveEmailMessage}";
                }

                return new ServiceResponse<string>("Created plan message", true, message);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<string>($"Could not GetSendPlanMessage {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
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

        private async Task<bool> IsLiveEmail()
        {
            string configValue = (await _configurationDataService.GetByIdAsync(ConfigNames.EmailUseFileMock)).Data.ConfigurationValue;
            return configValue != "true";
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
                var dbSet = ctx.Set<EmailQueue>();
                var result = await dbSet.CountAsync(o => o.Status == EmailQueueStatus.Queued.ToString());
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }

        public async Task<ServiceResponse<List<string>>> GetEmailsToSend(int locationId, EmailRecipientOption option, int scheduleId)
        {
            const string message = "Built Email List";
            switch (option)
            {
                case EmailRecipientOption.Myself:
                    return new ServiceResponse<List<string>>(message, true, (await GetMyself()));
                case EmailRecipientOption.Everyone:
                    return new ServiceResponse<List<string>>(message, true, (await GetEveryone()));
                case EmailRecipientOption.VolunteersForLocation:
                    return new ServiceResponse<List<string>>(message, true, (await _volunteerDataService.GetDistinctEmailByLocation(locationId)).Data);
                case EmailRecipientOption.BedRequestorsForLocation:
                    return new ServiceResponse<List<string>>(message, true, (await _bedRequestDataService.GetDistinctEmailByLocation(locationId)).Data);
                case EmailRecipientOption.ContactUsForLocation:
                    return new ServiceResponse<List<string>>(message, true, (await _contactUsDataService.GetDistinctEmailByLocation(locationId)).Data);
                case EmailRecipientOption.BedBrigadeLeadersNationwide:
                    return new ServiceResponse<List<string>>(message, true, (await _userDataService.GetDistinctEmail()).Data);
                case EmailRecipientOption.BedBrigadeLeadersForLocation:
                    return new ServiceResponse<List<string>>(message, true, (await _userDataService.GetDistinctEmailByLocation(locationId)).Data);
                case EmailRecipientOption.VolunteersWithDeliveryVehicles:
                    return new ServiceResponse<List<string>>(message, true, (await _volunteerDataService.GetVolunteerEmailsWithDeliveryVehicles(locationId)).Data);
                case EmailRecipientOption.VolunteersForAnEvent:
                    return new ServiceResponse<List<string>>(message, true, (await _volunteerDataService.GetVolunteerEmailsForASchedule(scheduleId)).Data);
                case EmailRecipientOption.BedRequestorsWhoHaveNotRecievedABed:
                    return new ServiceResponse<List<string>>(message, true, (await _bedRequestDataService.EmailsForNotReceivedABed(locationId)).Data);
                case EmailRecipientOption.BedRequestorsWhoHaveRecievedABed:
                    return new ServiceResponse<List<string>>(message, true, (await _bedRequestDataService.EmailsForReceivedABed(locationId)).Data);
                case EmailRecipientOption.BedRequestorsForAnEvent:
                    return new ServiceResponse<List<string>>(message, true, (await _bedRequestDataService.EmailsForSchedule(locationId)).Data);
                default:
                    throw new ArgumentOutOfRangeException(nameof(option), option, $"Unsupported Option: {option}");
            }
        }

        public async Task<string> GetEstimatedTime(int queueCount, int emailCount)
        {
            int totalCount = queueCount + emailCount;
            int maxEmailsPerMinute = Convert.ToInt32((await _configurationDataService.GetByIdAsync(ConfigNames.EmailMaxSendPerMinute)).Data.ConfigurationValue);
            int maxEmailsPerHour = Convert.ToInt32((await _configurationDataService.GetByIdAsync(ConfigNames.EmailMaxSendPerHour)).Data.ConfigurationValue);

            if (totalCount <= maxEmailsPerMinute)
            {
                return "1 minute";
            }

            if (totalCount <= maxEmailsPerHour)
            {
                return "2 minutes";
            }

            double hours = Convert.ToDouble(totalCount) / Convert.ToDouble(maxEmailsPerHour);
            long milliseconds = Convert.ToInt64(hours * (double) 60 * (double) 60 * (double)1000);
            return DateUtil.MillisecondsToTimeLapse(milliseconds);
        }

        private async Task<List<string>> GetMyself()
        {
            var user = await GetUserEmail();
            return new List<string> { user };
        }

        private async Task<List<string>> GetEveryone()
        {
            var volunteers = await _volunteerDataService.GetDistinctEmail();
            var bedRequestors = await _bedRequestDataService.GetDistinctEmail();
            var contactUs = await _contactUsDataService.GetDistinctEmail();
            var users = await _userDataService.GetDistinctEmail();

            var everyone = new List<string>();
            everyone.AddRange(volunteers.Data);
            everyone.AddRange(bedRequestors.Data);
            everyone.AddRange(contactUs.Data);
            everyone.AddRange(users.Data);
            return everyone.Distinct().ToList();
        }
    }
}
