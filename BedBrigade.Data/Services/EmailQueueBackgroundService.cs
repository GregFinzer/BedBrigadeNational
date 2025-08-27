using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Azure;
using System.Net.Mail;

namespace BedBrigade.Data.Services
{
    public class EmailQueueBackgroundService : BackgroundService
    {
        private IEmailQueueDataService _emailQueueDataService;
        private IConfigurationDataService _configurationDataService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailQueueBackgroundService> _logger;
        private bool _isProcessing = false;
        private bool _isStopping;
        private bool _sendNow;

        // Configuration fields
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
        private string _host;
        private int _port;
        private string _userName;
        private string _password;

        public EmailQueueBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<EmailQueueBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void StopService()
        {
            _isStopping = true;
        }

        public void SendNow()
        {
            _sendNow = true;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Initial delay before starting the service
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);

            while (!cancellationToken.IsCancellationRequested && !_isStopping)
            {
                if (!_isProcessing)
                {
                    _logger.LogInformation("Email Queue Background Service Executing");
                    try
                    {
                        _isProcessing = true;
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            _emailQueueDataService = scope.ServiceProvider.GetRequiredService<IEmailQueueDataService>();
                            _configurationDataService = scope.ServiceProvider.GetRequiredService<IConfigurationDataService>();

                            await LoadConfiguration();
                            await ProcessQueue(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while processing the email queue.");
                    }
                    finally
                    {
                        _isProcessing = false;
                        _logger.LogInformation("Email Queue Background Service Complete");
                    }
                }

                await WaitLoop(cancellationToken);
            }
        }

        private async Task WaitLoop(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 60; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (_isStopping)
                    return;

                if (_sendNow)
                {
                    _sendNow = false;
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private async Task LoadConfiguration()
        {
            _logger.LogDebug("EmailQueueLogic: LoadConfiguration");
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
            _emailUseFileMock =
                await _configurationDataService.GetConfigValueAsBoolAsync(ConfigSection.Email,
                    ConfigNames.EmailUseFileMock);
            _fromEmailAddress = await LoadEmailConfigString(ConfigNames.FromEmailAddress);
            _fromEmailDisplayName = await LoadEmailConfigString(ConfigNames.FromEmailDisplayName);
            _host = await LoadEmailConfigString(ConfigNames.EmailHost);
            _port = await LoadEmailConfigValue(ConfigNames.EmailPort);
            _userName = await LoadEmailConfigString(ConfigNames.EmailUserName);
            _password = await LoadEmailConfigString(ConfigNames.EmailPassword);
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

        private async Task<string> LoadEmailConfigString(string id)
        {
            var configResult = await _configurationDataService.GetByIdAsync(id);

            if (!configResult.Success || configResult.Data == null)
            {
                throw new ArgumentException("Could not find Config setting: " + id);
            }

            return configResult.Data.ConfigurationValue ?? string.Empty;
        }

        private async Task ProcessQueue(CancellationToken cancellationToken)
        {
            if (!TimeToSendEmail())
                return;

            if (cancellationToken.IsCancellationRequested)
                return;

            if (await MaxSent())
                return;

            if (cancellationToken.IsCancellationRequested)
                return;

            if (await WaitForLock())
                return;

            await SendQueuedEmails(cancellationToken);
            await _emailQueueDataService.DeleteOldEmailQueue(_emailKeepDays);
        }

        private bool TimeToSendEmail()
        {
            TimeZoneInfo defaultTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(Defaults.DefaultTimeZoneId);
            DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, defaultTimeZoneInfo);

            bool validTime = currentDate.Hour >= _emailBeginHour && currentDate.Hour <= _emailEndHour;
            bool validDay = currentDate.DayOfWeek >= (DayOfWeek)_emailBeginDayOfWeek && currentDate.DayOfWeek <= (DayOfWeek)_emailEndDayOfWeek;

            string message;

            if (!validDay)
            {
                message =
                    $"EmailQueueLogic:  Not Time to Send (valid days are {(DayOfWeek)_emailBeginDayOfWeek}->{(DayOfWeek)_emailEndDayOfWeek}): {currentDate.DayOfWeek}";
                _logger.LogDebug(message);
            }
            else if (!validTime)
            {
                message = string.Format("EmailQueueLogic:  Not Time to Send (valid hours are {0} to {1}): ",
                    _emailBeginHour, _emailEndHour);
                _logger.LogDebug(message);
            }

            return validDay && validTime;
        }

        private async Task<bool> MaxSent()
        {
            _logger.LogDebug("EmailQueueLogic:  GetEmailsSentToday");
            List<EmailSlim> emailsSentToday = await _emailQueueDataService.GetEmailsSentToday();

            if (emailsSentToday.Count(o => o.SentDate >= DateTime.UtcNow.AddMinutes(-1)) >= _emailMaxSendPerMinute)
            {
                _logger.LogDebug("EmailQueueLogic: Sent max per minute of " + _emailMaxSendPerMinute);
                return true;
            }

            if (emailsSentToday.Count(o => o.SentDate >= DateTime.UtcNow.AddHours(-1)) >= _emailMaxSendPerHour)
            {
                _logger.LogDebug("EmailQueueLogic: Sent max per hour of " + _emailMaxSendPerHour);
                return true;
            }

            if (emailsSentToday.Count >= _emailMaxSendPerDay)
            {
                _logger.LogDebug("EmailQueueLogic: Sent max per day of " + _emailMaxSendPerDay);
                return true;
            }

            return false;
        }

        private async Task<bool> WaitForLock()
        {
            _logger.LogDebug("EmailQueueLogic:  GetLockedEmails");
            List<EmailQueue> lockedEmails = await _emailQueueDataService.GetLockedEmails();

            EmailQueue firstLockedEmail = lockedEmails.FirstOrDefault();

            if (firstLockedEmail != null)
            {
                //If there are emails locked less than an hour ago, skip
                if (firstLockedEmail.LockDate.HasValue
                    && firstLockedEmail.LockDate.Value > DateTime.UtcNow.AddMinutes(_emailLockWaitMinutes * -1))
                {
                    _logger.LogDebug($"EmailQueueLogic:  Emails are currently locked, waiting for {_emailLockWaitMinutes} minutes");
                    return true;
                }

                //Clear locked because something bad happened and we want to retry
                await _emailQueueDataService.ClearEmailQueueLock();
            }

            return false;
        }

        private async Task SendQueuedEmails(CancellationToken cancellationToken)
        {
            List<EmailQueue> emailsToProcess = await _emailQueueDataService.GetEmailsToProcess(_emailMaxPerChunk);
            _logger.LogDebug("EmailQueueLogic:  Emails to send now: " + emailsToProcess.Count);

            if (emailsToProcess.Count == 0)
                return;

            if (!_emailUseFileMock)
            {
                _logger.LogDebug("EmailQueueLogic:  Sending LIVE emails");
            }

            _logger.LogDebug("EmailQueueLogic:  Locking Emails");
            await _emailQueueDataService.LockEmailsToProcess(emailsToProcess);

            foreach (var emailQueue in emailsToProcess)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await _emailQueueDataService.ClearEmailQueueLock();
                    return;
                }

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
            _logger.LogDebug(msg);
        }

        private async Task LogEmail(EmailQueue email)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Send Email (nothing actually sent see Configuration.EmailUseFileMock");
            sb.AppendLine($"From:  {email.FromAddress}");
            sb.AppendLine($"From Display Name:  {email.FromDisplayName}");
            sb.AppendLine($"To:  {email.ToAddress}");
            sb.AppendLine($"Subject:  {email.Subject}");
            sb.AppendLine($"Body:  {email.Body}");
            sb.AppendLine("=".PadRight(40, '='));
            _logger.LogInformation(sb.ToString());

            email.LockDate = null;
            email.SentDate = DateTime.UtcNow;
            email.Status = EmailQueueStatus.Sent.ToString();
            await _emailQueueDataService.UpdateAsync(email);
        }

        private async Task SendLiveEmail(EmailQueue email)
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
                    $"Email send operation failed with error code: {ex.ErrorCode}, message: {ex}";
                email.Status = EmailQueueStatus.Failed.ToString();
            }
            catch (Exception ex)
            {
                email.FailureMessage =
                    $"Email send operation failed, message: {ex}";
                email.Status = EmailQueueStatus.Failed.ToString();
            }

            email.LockDate = null;
            email.SentDate = DateTime.UtcNow;
            await _emailQueueDataService.UpdateAsync(email);
        }
    }
}
