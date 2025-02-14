#region Includes
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#endregion

namespace BedBrigade.Data.Services;

public class SmsQueueBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmsQueueBackgroundService> _logger;

    #region Class Variables

    private ISmsQueueDataService _smsQueueDataService;
    private IConfigurationDataService _configurationDataService;

    private ISendSmsLogic _sendSmsLogic;
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
    private int _smsAppointmentReminderHour;
    private bool _isStopping;

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

            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }


    #endregion

    #region Private Methods

    private async Task ProcessQueue(CancellationToken cancellationToken)
    {

        //We only send between _smsBeginDayOfWeek through _smsEndDayOfWeek between _smsBeginHour and _smsEndHour
        if (!TimeToSendMessages())
            return;

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
        DateTime currentTime = DateTime.Now;
        int millisecondDelay = 1000 / _smsMaxSendPerSecond;

        foreach (var smsQueue in messagesToProcess)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await _smsQueueDataService.ClearSmsQueueLock();
                return;
            }

            var result = await _sendSmsLogic.SendTextMessage(smsQueue);

            if (!result.Success)
                continue;

            var elapsed = DateTime.Now - currentTime;
            if (elapsed.TotalMilliseconds < millisecondDelay)
            {
                int toWait = millisecondDelay - (int)elapsed.TotalMilliseconds;
                System.Threading.Thread.Sleep(toWait);
            }

            currentTime = DateTime.Now;
        }

        int total = messagesToProcess.Count;
        int sent = messagesToProcess.Count(o => o.Status == SmsQueueStatus.Sent.ToString());
        int failed = messagesToProcess.Count(o => o.Status == SmsQueueStatus.Failed.ToString());

        string msg = string.Format("{0} SMS messages sent, {1} SMS messages failed, {2} total SMS messages", sent, failed, total);
        _logger.LogDebug(msg);
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



    private bool TimeToSendMessages()
    {
        DateTime currentDate = DateTime.UtcNow;
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

