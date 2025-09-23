#region Includes

using System.Text;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#endregion

namespace BedBrigade.Data.Services;

public class SmsQueueBackgroundService : BackgroundService
{
    #region Class Variables
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmsQueueBackgroundService> _logger;
    private ISmsQueueDataService _smsQueueDataService;
    private IConfigurationDataService _configurationDataService;
    private ILocationDataService _locationDataService;
    private IUserDataService _userDataService;
    private IEmailQueueDataService _emailQueueDataService;
    private ISendSmsLogic _sendSmsLogic;
    private IConfiguration _configuration;
    private ITimezoneDataService _timezoneDataService;
    private bool _isProcessing = false;
    private int _smsBeginHour;
    private int _smsEndHour;
    private int _smsBeginDayOfWeek;
    private int _smsEndDayOfWeek;
    private int _smsMaxSendPerSecond;
    private int _smsLockWaitMinutes;
    private int _smsKeepDays;
    private int _smsMaxPerChunk;
    private bool _smsUseFileMock;
    private bool _isStopping;
    private bool _sendNow;
    #endregion

    public SmsQueueBackgroundService(IServiceProvider serviceProvider,
        ILogger<SmsQueueBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    #region Public Methods
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
                _logger.LogInformation("SMS Queue Background Service Executing");
                try
                {
                    _isProcessing = true;
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        _smsQueueDataService = scope.ServiceProvider.GetRequiredService<ISmsQueueDataService>();
                        _configurationDataService =
                            scope.ServiceProvider.GetRequiredService<IConfigurationDataService>();
                        _sendSmsLogic = scope.ServiceProvider.GetRequiredService<ISendSmsLogic>();
                        _locationDataService = scope.ServiceProvider.GetRequiredService<ILocationDataService>();
                        _userDataService = scope.ServiceProvider.GetRequiredService<IUserDataService>();
                        _emailQueueDataService = scope.ServiceProvider.GetRequiredService<IEmailQueueDataService>();
                        _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                        _timezoneDataService = scope.ServiceProvider.GetRequiredService<ITimezoneDataService>();
                        await LoadConfiguration();
                        await ProcessQueue(cancellationToken);
                        await CheckMissedMessages(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing the email queue.");
                }
                finally
                {
                    _isProcessing = false;
                    _logger.LogInformation("SMS Queue Background Service Complete");
                }
            }

            await WaitLoop(cancellationToken);
        }
    }


    #endregion

    #region Private Methods

    private async Task WaitLoop(CancellationToken cancellationToken)
    {
        for (int i=0; i < 60; i++)
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

    private async Task CheckMissedMessages(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        var missedMessages = await _smsQueueDataService.GetOldUnreadMessages();

        if (missedMessages.Success && missedMessages.Data != null && missedMessages.Data.Count > 0)
        {
            foreach (var smsQueue in missedMessages.Data)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (!smsQueue.SentDate.HasValue)
                    continue;

                var locationResult = await _locationDataService.GetByIdAsync(smsQueue.LocationId);

                if (locationResult.Success && locationResult.Data != null)
                {
                    smsQueue.SentDateLocal = _timezoneDataService.ConvertUtcToTimeZone(smsQueue.SentDate.Value, locationResult.Data.TimeZoneId);
                }
                else
                {
                    smsQueue.SentDateLocal = _timezoneDataService.ConvertUtcToTimeZone(smsQueue.SentDate.Value, Defaults.DefaultTimeZoneId);
                }

                string subject = BuildSubject(smsQueue);
                
                var sentToday = await _emailQueueDataService.GetEmailsSentToday();

                if (!sentToday.Any(o => o.Subject == subject))
                {
                    await SendMissedMessageEmails(smsQueue, subject);
                }
            }
        }

    }

    private async Task SendMissedMessageEmails(SmsQueue smsQueue, string subject)
    {
        List<string> emails = await _userDataService.GetMissedMessageEmailsForLocation(smsQueue.LocationId);

        foreach (var email in emails)
        {
            string body = BuildBody(smsQueue);
            EmailQueue emailQueue = new EmailQueue
            {
                ToAddress = email,
                Subject = subject,
                Body = body,
                QueueDate = DateTime.UtcNow,
                FailureMessage = string.Empty,
                Status = EmailQueueStatus.Queued.ToString(),
                Priority = Defaults.BulkMediumPriority
            };
            await _emailQueueDataService.QueueEmail(emailQueue);
            _logger.LogInformation($"Queued Missed SMS Message to Email: {email}:\r\n{body}");
        }
    }

    private string BuildBody(SmsQueue smsQueue)
    {
        string contactName = smsQueue.ContactName;

        if (String.IsNullOrEmpty(contactName))
        {
            contactName = smsQueue.ToPhoneNumber.FormatPhoneNumber();
        }

        StringBuilder sb = new StringBuilder(1024);
        string sentDateString = smsQueue.SentDateLocal.Value.ToString("f");
        sb.AppendLine($"You have a missed text message from {contactName} on {sentDateString}.");
        sb.AppendLine($"Phone: {smsQueue.ToPhoneNumber.FormatPhoneNumber()}");
        sb.AppendLine($"Contact Type: {smsQueue.ContactType}");
        sb.AppendLine();
        sb.AppendLine("Message:");
        sb.AppendLine(smsQueue.Body);
        sb.AppendLine();
        sb.AppendLine("Please respond to this message by going to the following URL:");
        string baseUrl = _configuration.GetSection("AppSettings:BaseUrl").Value.TrimEnd('/');
        string phoneHtmlEncoded = System.Web.HttpUtility.UrlPathEncode(smsQueue.ToPhoneNumber.FormatPhoneNumber());
        sb.AppendLine($"{baseUrl}/administration/SMS/SmsDetails/{smsQueue.LocationId}/{phoneHtmlEncoded}");
        sb.AppendLine();
        sb.AppendLine("You must go to this link to read the message or it will continue to send a reminder every day.");
        return sb.ToString();
    }

    private string BuildSubject(SmsQueue smsQueue)
    {
        string contactName = smsQueue.ContactName;

        if (String.IsNullOrEmpty(contactName))
        {
            contactName = smsQueue.ToPhoneNumber.FormatPhoneNumber();
        }

        return $"Missed Text Message from {contactName} on {smsQueue.SentDateLocal.Value.ToString("f")}";
    }

    private async Task ProcessQueue(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (await WaitForLock())
            return;

        await SendQueuedMessages(cancellationToken);
        await _smsQueueDataService.DeleteOldSmsQueue(_smsKeepDays);
    }

    private async Task SendQueuedMessages(CancellationToken cancellationToken)
    {
        List<SmsQueue> messagesToProcess = await _smsQueueDataService.GetMessagesToProcess(_smsMaxPerChunk);
        _logger.LogDebug("SmsQueueLogic:  Messages to send now: " + messagesToProcess.Count);

        if (messagesToProcess.Count == 0)
            return;

        if (!_smsUseFileMock)
        {
            _logger.LogDebug("SmsQueueLogic:  Sending LIVE Text Messages");
        }

        _logger.LogDebug("SmsQueueLogic:  Locking Messages");
        await _smsQueueDataService.LockMessagesToProcess(messagesToProcess);
        DateTime currentTime = DateTime.UtcNow;
        int millisecondDelay = 1000 / _smsMaxSendPerSecond;

        foreach (var smsQueue in messagesToProcess)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await _smsQueueDataService.ClearSmsQueueLock();
                return;
            }

            if (!(await TimeToSendMessage(smsQueue.LocationId)))
            {
                await UnlockMessage(smsQueue);
                continue;
            }

            if (String.IsNullOrEmpty(smsQueue.ContactName))
            {
                await _smsQueueDataService.FillContactByToPhoneNumber(smsQueue);
            }

            var result = await _sendSmsLogic.SendTextMessage(smsQueue);

            if (!result.Success)
                continue;

            var elapsed = DateTime.UtcNow - currentTime;
            if (elapsed.TotalMilliseconds < millisecondDelay)
            {
                int toWait = millisecondDelay - (int)elapsed.TotalMilliseconds;
                System.Threading.Thread.Sleep(toWait);
            }

            currentTime = DateTime.UtcNow;
        }

        int total = messagesToProcess.Count;
        int sent = messagesToProcess.Count(o => o.Status == SmsQueueStatus.Sent.ToString());
        int failed = messagesToProcess.Count(o => o.Status == SmsQueueStatus.Failed.ToString());

        string msg = string.Format("{0} SMS messages sent, {1} SMS messages failed, {2} total SMS messages", sent, failed, total);
        _logger.LogDebug(msg);
    }

    private async Task UnlockMessage(SmsQueue message)
    {
        message.Status = SmsQueueStatus.Queued.ToString();
        message.LockDate = null;
        message.UpdateDate = DateTime.UtcNow;
        await _smsQueueDataService.UpdateAsync(message);
    }




    private async Task<bool> WaitForLock()
    {
        _logger.LogDebug("SmsQueueLogic:  GetLockedMessages");
        List<SmsQueue> lockedMessages = await _smsQueueDataService.GetLockedMessages();

        SmsQueue firstLockedMessage = lockedMessages.FirstOrDefault();

        if (firstLockedMessage != null)
        {
            //If there are messages locked less than an hour ago, skip
            if (firstLockedMessage.LockDate.HasValue
                && firstLockedMessage.LockDate.Value > DateTime.UtcNow.AddMinutes(_smsLockWaitMinutes))
            {
                _logger.LogDebug($"SmsQueueLogic:  Messages are currently locked, waiting for {_smsLockWaitMinutes} minutes");
                return true;
            }

            //Clear locked because something bad happened and we want to retry
            await _smsQueueDataService.ClearSmsQueueLock();
        }

        return false;
    }



    private async Task<bool> TimeToSendMessage(int locationId)
    {
        TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(Defaults.DefaultTimeZoneId);
        var result = await _locationDataService.GetByIdAsync(locationId);

        if (result.Success && result.Data != null && !String.IsNullOrEmpty(result.Data.TimeZoneId))
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(result.Data.TimeZoneId);
        }

        DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

        bool validTime = currentDate.Hour >= _smsBeginHour && currentDate.Hour <= _smsEndHour;
        bool validDay = currentDate.DayOfWeek >= (DayOfWeek)_smsBeginDayOfWeek &&
                        currentDate.DayOfWeek <= (DayOfWeek)_smsEndDayOfWeek;

        string message;

        if (!validDay)
        {
            message =
                $"SmsQueueLogic:  Not Time to Send (valid days are {(DayOfWeek)_smsBeginDayOfWeek}->{(DayOfWeek)_smsEndDayOfWeek}): {currentDate.DayOfWeek}";
            _logger.LogDebug(message);
        }
        else if (!validTime)
        {
            message = string.Format("SmsQueueLogic:  Not Time to Send (valid hours are {0} to {1}): ",
                _smsBeginHour, _smsEndHour);
            _logger.LogDebug(message);
        }

        return validDay && validTime;
    }



    private async Task LoadConfiguration()
    {
        _logger.LogDebug("SmsQueueLogic: LoadConfiguration");
        _smsBeginHour = await LoadSmsConfigValue(ConfigNames.SmsBeginHour);
        _smsEndHour = await LoadSmsConfigValue(ConfigNames.SmsEndHour);
        _smsBeginDayOfWeek = await LoadSmsConfigValue(ConfigNames.SmsBeginDayOfWeek);
        _smsEndDayOfWeek = await LoadSmsConfigValue(ConfigNames.SmsEndDayOfWeek);
        _smsMaxSendPerSecond = await LoadSmsConfigValue(ConfigNames.SmsMaxSendPerSecond);
        _smsLockWaitMinutes = await LoadSmsConfigValue(ConfigNames.SmsLockWaitMinutes);
        _smsKeepDays = await LoadSmsConfigValue(ConfigNames.SmsKeepDays);
        _smsMaxPerChunk = await LoadSmsConfigValue(ConfigNames.SmsMaxPerChunk);
        _smsUseFileMock =
            await _configurationDataService.GetConfigValueAsBoolAsync(ConfigSection.Sms,
                ConfigNames.SmsUseFileMock);
        

    }

    private async Task<int> LoadSmsConfigValue(string id)
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

