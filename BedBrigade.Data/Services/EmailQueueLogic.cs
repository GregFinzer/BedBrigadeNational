using System.Text;
using Azure;
using Azure.Communication.Email;
using BedBrigade.Common;
using BedBrigade.Data.Models;
using Serilog;

namespace BedBrigade.Data.Services
{
    public  class EmailQueueLogic :IEmailQueueLogic
    {
        private readonly IEmailQueueDataService _emailQueueDataService;
        private readonly IConfigurationDataService _configurationDataService;
        private readonly Timer _timer;
        private bool _processing = false;
        private int _emailBeginHour;
        private int _emailEndHour;
        private int _emailBeginDayOfWeek;
        private int _emailEndDayOfWeek;
        private int _emailMaxSendPerMinute;
        private int _emailMaxSendPerHour;
        private int _emailMaxSendPerDay;
        private int _emailLockWaitMinutes;
        private int _emailKeepDays;
        private int _emailMaxPerChunk;
        private bool _emailUseFileMock;
        private string _fromEmailAddress;
        private string _fromEmailDisplayName;

        public EmailQueueLogic(IEmailQueueDataService emailQueueDataService, IConfigurationDataService configurationDataService)
        {
            _emailQueueDataService = emailQueueDataService;
            _configurationDataService = configurationDataService;
            const int oneMinute = 1000 * 60;
            //_timer = new Timer(ProcessQueue, null, oneMinute, oneMinute);
        }

        public async void ProcessQueue(object? state)
        {
            if (_processing)
                return;

            _processing = true;
            await LoadConfiguration();

            //We only send between _emailBeginDayOfWeek through _emailEndDayOfWeek between _beginHour and _endHour
            if (!TimeToSendEmail())
            {
                _processing = false;
                return;
            }

            //If we sent the max per hour or max per day, exit
            if (await MaxSent())
            {
                _processing = false;
                return;
            }

            if (await WaitForLock())
            {
                _processing = false;
                return;
            }

            await SendQueuedEmails();
            await _emailQueueDataService.DeleteOldEmailQueue(_emailKeepDays);
            _processing = false;
        }

        private async Task SendQueuedEmails()
        {
            List<EmailQueue> emailsToProcess = await _emailQueueDataService.GetEmailsToProcess(_emailMaxPerChunk);
            Log.Debug("EmailQueueLogic:  Emails to send now: " + emailsToProcess.Count);

            Log.Debug("EmailQueueLogic:  Locking Emails");
            await _emailQueueDataService.LockEmailsToProcess(emailsToProcess);

            foreach (var emailQueue in emailsToProcess)
            {
                emailQueue.FromAddress = _fromEmailAddress;
                emailQueue.FromDisplayName = _fromEmailDisplayName;

                if (_emailUseFileMock)
                {
                    await LogEmail(emailQueue);
                }
                else
                {
                    await SendLiveEmail(emailQueue);
                }
            }

            int total = emailsToProcess.Count;
            int sent = emailsToProcess.Count(o => o.Status == EmailQueueStatus.Sent.ToString());
            int failed = emailsToProcess.Count(o => o.Status == EmailQueueStatus.Failed.ToString());

            string msg = string.Format("{0} emails sent, {1} emails failed, {2} total emails", sent, failed, total);
            Log.Debug(msg);
        }

        private async Task LogEmail(EmailQueue email)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Send Email");
            sb.AppendLine($"From:  {email.FromAddress}");
            sb.AppendLine($"From Display Name:  {email.FromDisplayName}");
            sb.AppendLine($"To:  {email.ToAddress}");
            sb.AppendLine($"Subject:  {email.Subject}");
            sb.AppendLine($"Body:  {email.Body}");
            sb.AppendLine("=".PadRight(40,'='));
            Log.Information(sb.ToString());

            email.SentDate = DateTime.Now;
            email.Status = EmailQueueStatus.Sent.ToString();
            await _emailQueueDataService.UpdateAsync(email);
        }

        private async Task SendLiveEmail(EmailQueue email)
        {
            // This code retrieves your connection string from an environment variable.
            string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");

            if (String.IsNullOrEmpty(connectionString))
            {
                string errorMessage =
                    "EmailQueueLogic:  COMMUNICATION_SERVICES_CONNECTION_STRING is not set as an environment variable";
                Log.Error(errorMessage);
                email.SentDate = DateTime.Now;
                email.FailureMessage = errorMessage;
                email.Status = EmailQueueStatus.Failed.ToString();
                await _emailQueueDataService.UpdateAsync(email);
                return;
            }

            var emailClient = new EmailClient(connectionString);

            EmailContent emailContent = new EmailContent(email.Subject);
            emailContent.PlainText = email.Body;
            EmailMessage emailMessage = new EmailMessage(email.FromAddress, email.ToAddress, emailContent);
            EmailAddress sender = new EmailAddress(email.FromAddress,email.FromDisplayName ?? email.FromAddress);
            emailMessage.ReplyTo.Add(sender);

            try
            {
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);


                if (!emailSendOperation.HasValue)
                {
                    return;
                }

                if (emailSendOperation.Value.Status == EmailSendStatus.Succeeded)
                {
                    email.Status = EmailQueueStatus.Sent.ToString();
                }
                else
                {
                    email.FailureMessage = emailSendOperation.GetRawResponse().ToString();
                    email.Status = EmailQueueStatus.Failed.ToString();
                }
            }
            catch (RequestFailedException ex)
            {
                email.FailureMessage =
                    $"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}";
                email.Status = EmailQueueStatus.Failed.ToString();
            }

            email.SentDate = DateTime.Now;
            await _emailQueueDataService.UpdateAsync(email);
        }

        private async Task<bool> WaitForLock()
        {
            Log.Debug("EmailQueueLogic:  GetLockedEmails");
            List<EmailQueue> lockedEmails = await _emailQueueDataService.GetLockedEmails();

            EmailQueue firstLockedEmail = lockedEmails.FirstOrDefault();

            if (firstLockedEmail != null)
            {
                //If there are emails locked less than an hour ago, skip
                if (firstLockedEmail.LockDate.HasValue
                    && firstLockedEmail.LockDate.Value > DateTime.Now.AddMinutes(_emailLockWaitMinutes))
                {
                    Log.Debug($"EmailQueueLogic:  Emails are currently locked, waiting for {_emailLockWaitMinutes} minutes");
                    return true;
                }

                //Clear locked because something bad happened and we want to retry
                await _emailQueueDataService.ClearEmailQueueLock();
            }

            return false;
        }

        private async Task<bool> MaxSent()
        {
            Log.Debug("EmailQueueLogic:  GetEmailsSentToday");
            List<EmailQueue> emailsSentToday = await _emailQueueDataService.GetEmailsSentToday();

            if (emailsSentToday.Count(o => o.SentDate >= DateTime.Now.AddMinutes(-1)) >= _emailMaxSendPerMinute)
            {
                Log.Debug("EmailQueueLogic: Sent max per minute of " + _emailMaxSendPerMinute);
                return true;
            }

            if (emailsSentToday.Count(o => o.SentDate >= DateTime.Now.AddHours(-1)) >= _emailMaxSendPerHour)
            {
                Log.Debug("EmailQueueLogic: Sent max per hour of " + _emailMaxSendPerHour);
                return true;
            }

            if (emailsSentToday.Count >= _emailMaxSendPerDay)
            {
                Log.Debug("EmailQueueLogic: Sent max per day of " + _emailMaxSendPerDay);
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
                email.Status = EmailQueueStatus.Queued.ToString();
                email.QueueDate = DateTime.Now;
                email.FailureMessage = string.Empty;
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
            Log.Debug("EmailQueueLogic: LoadConfiguration");
            _emailBeginHour = await LoadEmailConfigValue(ConfigNames.EmailBeginHour);
            _emailEndHour = await LoadEmailConfigValue(ConfigNames.EmailEndHour);
            _emailBeginDayOfWeek = await LoadEmailConfigValue(ConfigNames.EmailBeginDayOfWeek);
            _emailEndDayOfWeek = await LoadEmailConfigValue(ConfigNames.EmailEndDayOfWeek);
            _emailMaxSendPerMinute = await LoadEmailConfigValue(ConfigNames.EmailMaxSendPerMinute);
            _emailMaxSendPerHour = await LoadEmailConfigValue(ConfigNames.EmailMaxSendPerHour);
            _emailMaxSendPerDay = await LoadEmailConfigValue(ConfigNames.EmailMaxSendPerDay);
            _emailLockWaitMinutes = await LoadEmailConfigValue(ConfigNames.EmailLockWaitMinutes);
            _emailKeepDays = await LoadEmailConfigValue(ConfigNames.EmailKeepDays);
            _emailMaxPerChunk = await LoadEmailConfigValue(ConfigNames.EmailMaxPerChunk);
            _emailUseFileMock = await LoadEmailConfigValue(ConfigNames.EmailUseFileMock) == 1;
            _fromEmailAddress = await LoadEmailConfigString(ConfigNames.FromEmailAddress);
            _fromEmailDisplayName = await LoadEmailConfigString(ConfigNames.FromEmailDisplayName);
        }

        private async Task<string> LoadEmailConfigString(string id)
        {
            var configResult = await _configurationDataService.GetByIdAsync(id);

            if (!configResult.Success || configResult.Data == null)
            {
                throw new ArgumentException("Could not find Config setting: " + id);
            }

            return configResult.Data.ConfigurationValue ?? string.Empty;
        }

        private async Task<int> LoadEmailConfigValue(string id)
        {
            var configResult = await _configurationDataService.GetByIdAsync(id);

            if (!configResult.Success || configResult.Data == null)
            {
                throw new ArgumentException("Could not find Config setting: " + id);
            }

            if ((configResult.Data.ConfigurationValue ?? string.Empty) == "true")
            {
                return 1;
            }

            if ((configResult.Data.ConfigurationValue ?? string.Empty) == "false")
            {
                return 0;
            }

            if (int.TryParse(configResult.Data.ConfigurationValue, out int response))
                return response;

            throw new ArgumentException(
                $"Expected Config setting {id} to be an integer: {configResult.Data.ConfigurationValue}");
        }
    }
}
