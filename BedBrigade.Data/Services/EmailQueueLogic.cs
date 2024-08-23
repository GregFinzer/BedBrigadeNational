﻿#region Includes
using System.Net.Mail;
using System.Text;
using Azure;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Serilog;
#endregion

namespace BedBrigade.Data.Services
{
    public static class EmailQueueLogic 
    {
        #region Class Variables

        private static IEmailQueueDataService _emailQueueDataService;
        private static IConfigurationDataService _configurationDataService;
        private static Timer _timer;
        private static bool _processing = false;
        private static int _emailBeginHour;
        private static int _emailEndHour;
        private static int _emailBeginDayOfWeek;
        private static int _emailEndDayOfWeek;
        private static int _emailMaxSendPerMinute;
        private static int _emailMaxSendPerHour;
        private static int _emailMaxSendPerDay;
        private static int _emailLockWaitMinutes;
        private static int _emailKeepDays;
        private static int _emailMaxPerChunk;
        private static bool _emailUseFileMock;
        private static string _fromEmailAddress;
        private static string _fromEmailDisplayName;
        private static string _host;
        private static int _port;
        private static string _userName;
        private static string _password;
        #endregion

        #region Public Methods

        public static void Start(IEmailQueueDataService emailQueueDataService, IConfigurationDataService configurationDataService)
        {
            _emailQueueDataService = emailQueueDataService;
            _configurationDataService = configurationDataService;
            const int oneMinute = 1000 * 60;
            _timer = new Timer(ProcessQueue, null, oneMinute, oneMinute);
        }

        public static async Task<ServiceResponse<string>> QueueBulkEmail(List<string> emaiList, string subject,
            string body)
        {
            if (emaiList.Count == 0)
                return new ServiceResponse<string>("No emails to send", false);

            try
            {
                foreach (string email in emaiList)
                {
                    EmailQueue emailQueue = new EmailQueue
                    {
                        ToAddress = email,
                        Subject = subject,
                        Body = body,
                        Status = EmailQueueStatus.Queued.ToString(),
                        QueueDate = DateTime.UtcNow,
                        FailureMessage = string.Empty
                    };
                    await _emailQueueDataService.CreateAsync(emailQueue);
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>(ex.Message, false);
            }

            return new ServiceResponse<string>(EmailQueueStatus.Queued.ToString(), true);
        }
        
        public static async Task<ServiceResponse<string>> QueueEmail(EmailQueue email)
        {
            try
            {
                email.Status = EmailQueueStatus.Queued.ToString();
                email.QueueDate = DateTime.UtcNow;
                email.FailureMessage = string.Empty;
                await _emailQueueDataService.CreateAsync(email);
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>(ex.Message, false);
            }

            return new ServiceResponse<string>(EmailQueueStatus.Queued.ToString(), true);
        }


        #endregion

        #region Private Methods
        private static async void ProcessQueue(object? state)
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

        private static async Task SendQueuedEmails()
        {
            List<EmailQueue> emailsToProcess = await _emailQueueDataService.GetEmailsToProcess(_emailMaxPerChunk);
            Log.Debug("EmailQueueLogic:  Emails to send now: " + emailsToProcess.Count);

            if (emailsToProcess.Count == 0)
                return;

            if (!_emailUseFileMock)
            {
                Log.Debug("EmailQueueLogic:  Sending LIVE emails");
            }

            Log.Debug("EmailQueueLogic:  Locking Emails");
            await _emailQueueDataService.LockEmailsToProcess(emailsToProcess);

            foreach (var emailQueue in emailsToProcess)
            {
                if (string.IsNullOrEmpty(emailQueue.FromAddress))
                {
                    emailQueue.FromAddress = _fromEmailAddress;
                }

                if (string.IsNullOrEmpty(emailQueue.FromDisplayName))
                {
                    emailQueue.FromDisplayName = _fromEmailDisplayName;
                }


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

        private static async Task LogEmail(EmailQueue email)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Send Email (nothing actually sent see Configuration.EmailUseFileMock");
            sb.AppendLine($"From:  {email.FromAddress}");
            sb.AppendLine($"From Display Name:  {email.FromDisplayName}");
            sb.AppendLine($"To:  {email.ToAddress}");
            sb.AppendLine($"Subject:  {email.Subject}");
            sb.AppendLine($"Body:  {email.Body}");
            sb.AppendLine("=".PadRight(40,'='));
            Log.Information(sb.ToString());

            email.LockDate = null;
            email.SentDate = DateTime.UtcNow;
            email.Status = EmailQueueStatus.Sent.ToString();
            await _emailQueueDataService.UpdateAsync(email);
        }

        private static async Task SendLiveEmail(EmailQueue email)
        {
            // Create the email message
            MailMessage mailMessage = new MailMessage();

            if (!string.IsNullOrEmpty(email.FromDisplayName))
            {
                mailMessage.From = new MailAddress(email.FromAddress, email.FromDisplayName);
            }
            else
            {
                mailMessage.From = new MailAddress(email.FromAddress);
            }

            if (!string.IsNullOrEmpty(email.ReplyToAddress))
            {
                if (!string.IsNullOrEmpty(email.FromDisplayName))
                {
                    mailMessage.ReplyToList.Add(new MailAddress(email.ReplyToAddress, email.FromDisplayName));
                }
                else
                {
                    mailMessage.ReplyToList.Add(new MailAddress(email.ReplyToAddress));
                }
            }

            if (!string.IsNullOrEmpty(email.ToDisplayName))
            {
                mailMessage.To.Add(new MailAddress(email.ToAddress, email.ToDisplayName));
            }
            else
            {
                mailMessage.To.Add(new MailAddress(email.ToAddress));
            }

            mailMessage.Subject = email.Subject;
            mailMessage.Body = email.Body;

            // Send the email
            SmtpClient smtpClient = new SmtpClient(_host, _port);


            try
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(_userName, _password);
                smtpClient.Send(mailMessage);
                email.Status = EmailQueueStatus.Sent.ToString();
            }
            catch (RequestFailedException ex)
            {
                email.FailureMessage =
                    $"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}";
                email.Status = EmailQueueStatus.Failed.ToString();
            }

            email.LockDate = null;
            email.SentDate = DateTime.UtcNow;
            await _emailQueueDataService.UpdateAsync(email);
        }

        private static async Task<bool> WaitForLock()
        {
            Log.Debug("EmailQueueLogic:  GetLockedEmails");
            List<EmailQueue> lockedEmails = await _emailQueueDataService.GetLockedEmails();

            EmailQueue firstLockedEmail = lockedEmails.FirstOrDefault();

            if (firstLockedEmail != null)
            {
                //If there are emails locked less than an hour ago, skip
                if (firstLockedEmail.LockDate.HasValue
                    && firstLockedEmail.LockDate.Value > DateTime.UtcNow.AddMinutes(_emailLockWaitMinutes))
                {
                    Log.Debug($"EmailQueueLogic:  Emails are currently locked, waiting for {_emailLockWaitMinutes} minutes");
                    return true;
                }

                //Clear locked because something bad happened and we want to retry
                await _emailQueueDataService.ClearEmailQueueLock();
            }

            return false;
        }

        private static async Task<bool> MaxSent()
        {
            Log.Debug("EmailQueueLogic:  GetEmailsSentToday");
            List<EmailQueue> emailsSentToday = await _emailQueueDataService.GetEmailsSentToday();

            if (emailsSentToday.Count(o => o.SentDate >= DateTime.UtcNow.AddMinutes(-1)) >= _emailMaxSendPerMinute)
            {
                Log.Debug("EmailQueueLogic: Sent max per minute of " + _emailMaxSendPerMinute);
                return true;
            }

            if (emailsSentToday.Count(o => o.SentDate >= DateTime.UtcNow.AddHours(-1)) >= _emailMaxSendPerHour)
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

        private static bool TimeToSendEmail()
        {
            DateTime currentDate = DateTime.UtcNow;
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



        private static async Task LoadConfiguration()
        {
            Log.Debug("EmailQueueLogic: LoadConfiguration");
            _emailBeginHour = await LoadEmailConfigValue( ConfigNames.EmailBeginHour);
            _emailEndHour = await LoadEmailConfigValue( ConfigNames.EmailEndHour);
            _emailBeginDayOfWeek = await LoadEmailConfigValue( ConfigNames.EmailBeginDayOfWeek);
            _emailEndDayOfWeek = await LoadEmailConfigValue( ConfigNames.EmailEndDayOfWeek);
            _emailMaxSendPerMinute = await LoadEmailConfigValue( ConfigNames.EmailMaxSendPerMinute);
            _emailMaxSendPerHour = await LoadEmailConfigValue( ConfigNames.EmailMaxSendPerHour);
            _emailMaxSendPerDay = await LoadEmailConfigValue( ConfigNames.EmailMaxSendPerDay);
            _emailLockWaitMinutes = await LoadEmailConfigValue( ConfigNames.EmailLockWaitMinutes);
            _emailKeepDays = await LoadEmailConfigValue( ConfigNames.EmailKeepDays);
            _emailMaxPerChunk = await LoadEmailConfigValue( ConfigNames.EmailMaxPerChunk);
            _emailUseFileMock =
                await _configurationDataService.GetConfigValueAsBoolAsync(ConfigSection.Email,
                    ConfigNames.EmailUseFileMock);
            _fromEmailAddress = await LoadEmailConfigString( ConfigNames.FromEmailAddress);
            _fromEmailDisplayName = await LoadEmailConfigString( ConfigNames.FromEmailDisplayName);
            _host = await LoadEmailConfigString(ConfigNames.EmailHost);
            _port = await LoadEmailConfigValue(ConfigNames.EmailPort);
            _userName = await LoadEmailConfigString(ConfigNames.EmailUserName);
            _password = await LoadEmailConfigString(ConfigNames.EmailPassword);
        }

        private static async Task<string> LoadEmailConfigString(string id)
        {
            var configResult = await _configurationDataService.GetByIdAsync(id);

            if (!configResult.Success || configResult.Data == null)
            {
                throw new ArgumentException("Could not find Config setting: " + id);
            }

            return configResult.Data.ConfigurationValue ?? string.Empty;
        }

        private static async Task<int> LoadEmailConfigValue(string id)
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
        #endregion
    }
}
