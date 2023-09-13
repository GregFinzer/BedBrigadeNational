using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common;
using BedBrigade.Data.Models;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;

namespace BedBrigade.Data.Services
{
    public  class EmailQueueLogic :IEmailQueueLogic
    {
        private readonly IEmailQueueDataService _emailQueueDataService;
        private readonly IConfigurationDataService _configurationDataService;
        private readonly Timer _timer;
        private bool _processing = false;
        private bool _configLoaded = false;
        private int _emailBeginHour;
        private int _emailEndHour;
        private int _emailBeginDayOfWeek;
        private int _emailEndDayOfWeek;
        private int _emailMaxSendPerHour;
        private int _emailMaxSendPerDay;
        private int _emailLockWaitMinutes;
        private int _emailKeepDays;
        private int _emailMaxPerChunk;

        public EmailQueueLogic(IEmailQueueDataService emailQueueDataService, IConfigurationDataService configurationDataService)
        {
            _emailQueueDataService = emailQueueDataService;
            _configurationDataService = configurationDataService;
            const int fiveMinutes = 1000 * 60 * 5;
            _timer = new Timer(ProcessQueue, null, fiveMinutes, fiveMinutes);
        }

        private async void ProcessQueue(object? state)
        {
            if (_processing)
                return;

            _processing = true;

            //We only send between _emailBeginDayOfWeek through _emailEndDayOfWeek between _beginHour and _endHour
            if (!TimeToSendEmail())
                return;

            //If we sent the max per hour or max per day, exit
            List<EmailQueue> emailsSentToday = await _emailQueueDataService.GetEmailsSentToday();

            if (MaxSent(emailsSentToday))
                return;
            
        }

        private bool MaxSent(List<EmailQueue> emailsSentToday)
        {
            if (emailsSentToday.Count >= _emailMaxSendPerDay)
            {
                Log.Debug("EmailQueueLogic: Sent max per day of " + _emailMaxSendPerDay);
                return true;
            }

            if (emailsSentToday.Count(o => o.SentDate >= DateTime.Now.AddHours(-1)) >= _emailMaxSendPerHour)
            {
                Log.Debug("EmailQueueLogic: Sent max per hour of " + _emailMaxSendPerHour);
                return true;
            }

            return false;
        }

        private bool TimeToSendEmail()
        {
            DateTime currentDate = DateTime.Now;
            bool validTime = currentDate.Hour >= _emailBeginHour && currentDate.Hour <= _emailEndHour;
            bool validDay = currentDate.DayOfWeek >= (DayOfWeek) _emailBeginDayOfWeek && currentDate.DayOfWeek <= (DayOfWeek)_emailEndDayOfWeek;

            string message;
            
            if (!validDay)
            {
                message =
                    $"EmailQueueLogic:  Not Time to Send (valid days are {(DayOfWeek)_emailBeginDayOfWeek}->{(DayOfWeek)_emailEndDayOfWeek}): {currentDate.DayOfWeek}";
                Log.Logger.Debug(message);
            }
            else if (!validTime)
            {
                 message= string.Format("EmailQueueLogic:  Not Time to Send (valid hours are {0} to {1}): ",
                    _emailBeginHour, _emailEndHour);
                Log.Debug(message);
            }

            return validDay && validTime;
        }

        public async Task<ServiceResponse<string>> QueueEmail(EmailQueue email)
        {
            try
            {
                await LoadConfiguration();

                email.Status = EmailQueueStatus.Queued.ToString();
                email.QueueDate = DateTime.Now;
                await _emailQueueDataService.CreateAsync(email);
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>(ex.Message, false);
            }
            
            return new ServiceResponse<string>(EmailQueueStatus.Queued.ToString(), true);
        }

        private async Task LoadConfiguration()
        {
            if (_configLoaded)
            {
                return;
            }
            _configLoaded = true;
            _emailBeginHour = await LoadEmailConfigValue(ConfigNames.EmailBeginHour);
            _emailEndHour = await LoadEmailConfigValue(ConfigNames.EmailEndHour);
            _emailBeginDayOfWeek = await LoadEmailConfigValue(ConfigNames.EmailBeginDayOfWeek);
            _emailEndDayOfWeek = await LoadEmailConfigValue(ConfigNames.EmailEndDayOfWeek);
            _emailMaxSendPerHour = await LoadEmailConfigValue(ConfigNames.EmailMaxSendPerHour);
            _emailMaxSendPerDay = await LoadEmailConfigValue(ConfigNames.EmailMaxSendPerDay);
            _emailLockWaitMinutes = await LoadEmailConfigValue(ConfigNames.EmailLockWaitMinutes);
            _emailKeepDays = await LoadEmailConfigValue(ConfigNames.EmailKeepDays);
            _emailMaxPerChunk = await LoadEmailConfigValue(ConfigNames.EmailMaxPerChunk);
        }

        private async Task<int> LoadEmailConfigValue(string id)
        {
            var configResult = await _configurationDataService.GetByIdAsync(id);

            if (!configResult.Success || configResult.Data == null)
            {
                throw new ArgumentException("Could not find Config setting: " + id);
            }

            if (int.TryParse(configResult.Data.ConfigurationValue, out int response))
                return response;

            throw new ArgumentException(
                $"Expected Config setting {id} to be an integer: {configResult.Data.ConfigurationValue}");
        }
    }
}
