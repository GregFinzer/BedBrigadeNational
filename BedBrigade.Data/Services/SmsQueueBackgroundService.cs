#region Includes
using System.Net.Mail;
using System.Text;
using System.Threading;
using Azure;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Log = Serilog.Log;

#endregion

namespace BedBrigade.Data.Services;

public class SmsQueueBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmsQueueBackgroundService> _logger;

    #region Class Variables

    private ISmsQueueDataService _smsQueueDataService;
    private IConfigurationDataService _configurationDataService;
    private IMailMergeLogic _mailMergeLogic;
    private ISignUpDataService _signUpDataService;
    private IVolunteerDataService _volunteerDataService;
    private ILocationDataService _locationDataService;
    private IScheduleDataService _scheduleDataService;
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
    private string _smsPhone;
    private int _smsAppointmentReminderHour;
    private string _smsAccountSid;
    private string _smsAuthToken;

    #endregion

    public SmsQueueBackgroundService(IServiceProvider serviceProvider,
        ILogger<SmsQueueBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    #region Public Methods

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Initial delay before starting the service
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
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
                        _mailMergeLogic = scope.ServiceProvider.GetRequiredService<IMailMergeLogic>();
                        _signUpDataService = scope.ServiceProvider.GetRequiredService<ISignUpDataService>();
                        _volunteerDataService = scope.ServiceProvider.GetRequiredService<IVolunteerDataService>();
                        _locationDataService = scope.ServiceProvider.GetRequiredService<ILocationDataService>();
                        _scheduleDataService = scope.ServiceProvider.GetRequiredService<IScheduleDataService>();

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

    //public static void Start(ISmsQueueDataService smsQueueDataService,
    //    IConfigurationDataService configurationDataService,
    //    IAppointmentService appointmentService,
    //    ICalendarService calendarService,
    //    IMailMergeLogic mailMergeLogic)
    //{
    //    _smsQueueDataService = smsQueueDataService;
    //    _configurationDataService = configurationDataService;
    //    _appointmentService = appointmentService;
    //    _calendarService = calendarService;
    //    _mailMergeLogic = mailMergeLogic;

    //    const int oneMinuteAndThirtySeconds = (1000 * 90);
    //    _timer = new Timer(ProcessQueue, null, oneMinuteAndThirtySeconds, oneMinuteAndThirtySeconds);
    //}



    //public static async Task<ServiceResponse<string>> QueueMessage(SmsQueue message)
    //{
    //    try
    //    {
    //        Appointment? appointment = null;

    //        if (message.AppointmentId.HasValue)
    //        {
    //            appointment = (await _appointmentService.GetByIdAsync(message.AppointmentId)).Data;
    //        }

    //        message.Status = SmsQueueStatus.Queued.ToString();
    //        message.QueueDate = DateTime.UtcNow;
    //        message.FailureMessage = string.Empty;

    //        if (appointment != null)
    //        {
    //            message.TargetDate = appointment.Start.Date.AddHours(_smsAppointmentReminderHour);
    //        }
    //        else
    //        {
    //            message.TargetDate = DateTime.Now;
    //        }

    //        await _smsQueueDataService.CreateAsync(message);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ServiceResponse<string>(ex.Message, false);
    //    }

    //    return new ServiceResponse<string>(SmsQueueStatus.Queued.ToString(), true);
    //}

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
        Log.Debug("SmsQueueLogic:  Messages to send now: " + messagesToProcess.Count);

        if (messagesToProcess.Count == 0)
            return;

        if (!_smsUseFileMock)
        {
            Log.Debug("SmsQueueLogic:  Sending LIVE Text Messages");
        }

        Log.Debug("SmsQueueLogic:  Locking Messages");
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

            try
            {
                await PerformMailMerge(smsQueue);
            }
            catch (Exception ex)
            {
                smsQueue.Status = SmsQueueStatus.Failed.ToString();
                smsQueue.FailureMessage = ex.Message;
                await _smsQueueDataService.UpdateAsync(smsQueue);
                Log.Error(ex, "Error merging SMS message");
                continue;
            }

            if (_smsUseFileMock)
            {
                await LogSmsMessage(smsQueue);
            }
            else
            {
                await SendLiveTextMessage(smsQueue);
            }

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

        string msg = string.Format("{0} SMS messages sent, {1} SMS messages failed, {2} total SMS messages", sent,
            failed, total);
        Log.Debug(msg);
    }

    private async Task PerformMailMerge(SmsQueue message)
    {
        if (message.SignUpId.HasValue)
        {
            ServiceResponse<SignUp> signUpResponse = await _signUpDataService.GetByIdAsync(message.SignUpId.Value);
            if (!signUpResponse.Success || signUpResponse.Data == null)
            {
                _logger.LogError("Error getting SignUp for SMS message: {0}", signUpResponse.Message);
            }

            ServiceResponse<Volunteer> volunteerResponse =
                await _volunteerDataService.GetByIdAsync(signUpResponse.Data.VolunteerId);

            if (!volunteerResponse.Success || volunteerResponse.Data == null)
            {
                _logger.LogError("Error getting Volunteer for SMS message: {0}", volunteerResponse.Message);
            }

            ServiceResponse<Location> locationResponse =
                await _locationDataService.GetByIdAsync(signUpResponse.Data.LocationId);

            if (!locationResponse.Success || locationResponse.Data == null)
            {
                _logger.LogError("Error getting Location for SMS message: {0}", locationResponse.Message);
            }

            ServiceResponse<Schedule> scheduleResponse =
                await _scheduleDataService.GetByIdAsync(signUpResponse.Data.ScheduleId);

            if (!scheduleResponse.Success || scheduleResponse.Data == null)
            {
                _logger.LogError("Error getting Schedule for SMS message: {0}", scheduleResponse.Message);
            }

            StringBuilder sb = new StringBuilder(message.Body, message.Body.Length * 2);
            sb = _mailMergeLogic.ReplaceVolunteerFields(volunteerResponse.Data, scheduleResponse.Data, sb);
            sb = _mailMergeLogic.ReplaceLocationFields(locationResponse.Data, sb);
            sb = _mailMergeLogic.ReplaceScheduleFields(scheduleResponse.Data, sb);
            message.Body = sb.ToString().Trim();
        }
    }


    private async Task LogSmsMessage(SmsQueue message)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Send Text Message (nothing actually sent see Configuration.SmsUseFileMock");
        sb.AppendLine($"FromPhoneNumber:  {message.FromPhoneNumber}");
        sb.AppendLine($"ToPhoneNumber:  {message.ToPhoneNumber}");
        sb.AppendLine($"Body:  {message.Body}");
        sb.AppendLine("=".PadRight(40, '='));
        Log.Information(sb.ToString());

        message.SentDate = DateTime.UtcNow;
        message.Status = SmsQueueStatus.Sent.ToString();
        await _smsQueueDataService.UpdateAsync(message);
    }

    private async Task SendLiveTextMessage(SmsQueue message)
    {
        try
        {
            string accountSid =
                await _configurationDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsAccountSid);

            string authToken =
                await _configurationDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsAuthToken);

            string fromPhone =
                await _configurationDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsPhone);

            TwilioClient.Init(accountSid, authToken);

            CreateMessageOptions messageOptions =
                new CreateMessageOptions(new PhoneNumber(StringUtil.FilterNumeric(message.ToPhoneNumber)));
            messageOptions.From = new PhoneNumber(fromPhone);
            messageOptions.Body = message.Body;

            MessageResource? result = await MessageResource.CreateAsync(messageOptions);

            if (result == null)
            {
                message.FailureMessage =
                    $"SMS send operation failed: result is null";
                message.Status = EmailQueueStatus.Failed.ToString();
            }
            else if (result.ErrorCode.HasValue && result.ErrorCode.Value > 0)
            {
                message.FailureMessage =
                    $"SMS send operation failed with error code: {result.ErrorCode}, message: {result.ErrorMessage}";
                message.Status = EmailQueueStatus.Failed.ToString();
            }
            else
            {
                message.Status = EmailQueueStatus.Sent.ToString();
            }
        }
        catch (Exception ex)
        {
            message.FailureMessage =
                $"SMS send operation failed: {ex.Message}";
            message.Status = EmailQueueStatus.Failed.ToString();
        }

        message.LockDate = null;
        message.SentDate = DateTime.UtcNow;
        await _smsQueueDataService.UpdateAsync(message);
    }








    private async Task<bool> WaitForLock()
    {
        Log.Debug("SmsQueueLogic:  GetLockedMessages");
        List<SmsQueue> lockedMessages = await _smsQueueDataService.GetLockedMessages();

        SmsQueue firstLockedMessage = lockedMessages.FirstOrDefault();

        if (firstLockedMessage != null)
        {
            //If there are messages locked less than an hour ago, skip
            if (firstLockedMessage.LockDate.HasValue
                && firstLockedMessage.LockDate.Value > DateTime.UtcNow.AddMinutes(_smsLockWaitMinutes))
            {
                Log.Debug($"SmsQueueLogic:  Messages are currently locked, waiting for {_smsLockWaitMinutes} minutes");
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
            Log.Logger.Debug(message);
        }
        else if (!validTime)
        {
            message = string.Format("SmsQueueLogic:  Not Time to Send (valid hours are {0} to {1}): ",
                _smsBeginHour, _smsEndHour);
            Log.Debug(message);
        }

        return validDay && validTime;
    }



    private async Task LoadConfiguration()
    {
        Log.Debug("SmsQueueLogic: LoadConfiguration");
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
        _smsPhone = await LoadSmsConfigString(ConfigNames.SmsPhone);
        _smsAccountSid = await LoadSmsConfigString(ConfigNames.SmsAccountSid);
        _smsAuthToken = await LoadSmsConfigString(ConfigNames.SmsAuthToken);
    }

    private async Task<string> LoadSmsConfigString(string id)
    {
        var configResult = await _configurationDataService.GetByIdAsync(id);

        if (!configResult.Success || configResult.Data == null)
        {
            throw new ArgumentException("Could not find Config setting: " + id);
        }

        return configResult.Data.ConfigurationValue ?? string.Empty;
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

