using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using System.Text;
using Serilog;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace BedBrigade.Data.Services;

public class SendSmsLogic : ISendSmsLogic
{
    private readonly IContentDataService _contentDataService;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IVolunteerDataService _volunteerDataService;
    private readonly ISmsQueueDataService _smsQueueDataService;
    private readonly IScheduleDataService _scheduleDataService;
    private ILocationDataService _locationDataService;
    private IMailMergeLogic _mailMergeLogic;
    private ISignUpDataService _signUpDataService;

    public SendSmsLogic(IContentDataService contentDataService, 
        IConfigurationDataService configurationDataService, 
        IVolunteerDataService volunteerDataService,
        ISmsQueueDataService smsQueueDataService,
        IScheduleDataService scheduleDataService, IMailMergeLogic mailMergeLogic, ISignUpDataService signUpDataService, ILocationDataService locationDataService)
    {
        _contentDataService = contentDataService;
        _configurationDataService = configurationDataService;
        _volunteerDataService = volunteerDataService;
        _smsQueueDataService = smsQueueDataService;
        _scheduleDataService = scheduleDataService;
        _mailMergeLogic = mailMergeLogic;
        _signUpDataService = signUpDataService;
        _locationDataService = locationDataService;
    }

    public async Task<ServiceResponse<bool>> CreateSignUpReminder(SignUp signUp)
    {
        ServiceResponse<Content> templateResult =
            await _contentDataService.GetByLocationAndContentType(signUp.LocationId,
                ContentType.SignUpSmsConfirmationForm);

        if (!templateResult.Success)
        {
            return new ServiceResponse<bool>("Failed to get SignUpSmsConfirmationForm: " + templateResult.Message);
        }

        string fromPhone;
        try
        {
            fromPhone = await _configurationDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsPhone,
                signUp.LocationId);
        }
        catch (Exception ex)
        {
            return new ServiceResponse<bool>(ex.Message);
        }

        ServiceResponse<Volunteer> volunteerResult = await _volunteerDataService.GetByIdAsync(signUp.VolunteerId);

        if (!volunteerResult.Success)
        {
            return new ServiceResponse<bool>($"Failed to get volunteer for {signUp.VolunteerId}: " +
                                             volunteerResult.Message);
        }

        ServiceResponse<Schedule> scheduleResult = await _scheduleDataService.GetByIdAsync(signUp.ScheduleId);
        if (!scheduleResult.Success)
        {
            return new ServiceResponse<bool>($"Failed to get schedule for {signUp.ScheduleId}: " +
                                             scheduleResult.Message);
        }

        SmsQueue smsQueue = BuildQueueRecord(signUp, fromPhone, volunteerResult, templateResult, scheduleResult);

        var createResult = await _smsQueueDataService.CreateAsync(smsQueue);
        if (createResult.Success)
        {
            return new ServiceResponse<bool>("SMS Message queued", true);
        }

        return new ServiceResponse<bool>("Failed to create SMS Queue: " + createResult.Message);
    }

    public async Task<ServiceResponse<bool>> SendTextMessage(int locationId, string phone, string body)
    {
        string fromPhoneNumber = await _configurationDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsPhone, locationId);
        SmsQueue smsQueue = new SmsQueue()
        {
            FromPhoneNumber = fromPhoneNumber.FormatPhoneNumber(),
            ToPhoneNumber = phone.FormatPhoneNumber(),
            Body = body,
            Priority = 1,
            Status = SmsQueueStatus.Locked.ToString(),
            QueueDate = DateTime.UtcNow,
            FailureMessage = string.Empty,
            TargetDate = DateTime.UtcNow,
            IsRead = true,
            IsReply = false,
            LocationId = locationId
        };

        await _smsQueueDataService.FillContactByToPhoneNumber(smsQueue);
        return await SendTextMessage(smsQueue);
    }

    private static SmsQueue BuildQueueRecord(SignUp signUp, string fromPhone, 
        ServiceResponse<Volunteer> volunteerResult,
        ServiceResponse<Content> templateResult, 
        ServiceResponse<Schedule> scheduleResult)
    {
        SmsQueue smsQueue = new SmsQueue()
        {
            SignUpId = signUp.SignUpId,
            FromPhoneNumber = fromPhone.FormatPhoneNumber(),
            ToPhoneNumber = volunteerResult.Data.Phone.FormatPhoneNumber(),
            Body = templateResult.Data.ContentHtml,
            Priority = Defaults.BulkHighPriority,
            Status = SmsQueueStatus.Queued.ToString(),
            QueueDate = DateTime.UtcNow,
            FailureMessage = string.Empty,
            TargetDate = scheduleResult.Data.EventDateScheduled.AddHours(-2),
            IsRead = true,
            IsReply = false,
            LocationId = signUp.LocationId,
            ContactType = ContactTypes.Volunteer,
            ContactName = volunteerResult.Data.FullName,
        };
        return smsQueue;
    }

    public async Task<ServiceResponse<bool>> SendTextMessage(SmsQueue smsQueue)
    {
        try
        {
            await PerformMailMerge(smsQueue);
        }
        catch (Exception ex)
        {
            await HandleFailure(smsQueue, ex);
            return new ServiceResponse<bool>(ex.Message);
        }

        bool smsUseFileMock =
            await _configurationDataService.GetConfigValueAsBoolAsync(ConfigSection.Sms,
                ConfigNames.SmsUseFileMock);

        ServiceResponse<bool> result;

        if (smsUseFileMock)
        {
            result = await LogSmsMessage(smsQueue);
        }
        else
        {
            result = await SendLiveTextMessage(smsQueue);
        }

        return result;
    }

    private async Task HandleFailure(SmsQueue smsQueue, Exception ex)
    {
        smsQueue.Status = SmsQueueStatus.Failed.ToString();
        smsQueue.FailureMessage = ex.Message;
        await _smsQueueDataService.UpdateAsync(smsQueue);
        Log.Error(ex, "Error merging SMS message");
    }

    private async Task PerformMailMerge(SmsQueue message)
    {
        if (message.SignUpId.HasValue)
        {
            ServiceResponse<SignUp> signUpResponse = await _signUpDataService.GetByIdAsync(message.SignUpId.Value);
            if (!signUpResponse.Success || signUpResponse.Data == null)
            {
                Log.Error("Error getting SignUp for SMS message: {0}", signUpResponse.Message);
            }

            ServiceResponse<Volunteer> volunteerResponse =
                await _volunteerDataService.GetByIdAsync(signUpResponse.Data.VolunteerId);

            if (!volunteerResponse.Success || volunteerResponse.Data == null)
            {
                Log.Error("Error getting Volunteer for SMS message: {0}", volunteerResponse.Message);
            }

            ServiceResponse<Location> locationResponse =
                await _locationDataService.GetByIdAsync(signUpResponse.Data.LocationId);

            if (!locationResponse.Success || locationResponse.Data == null)
            {
                Log.Error("Error getting Location for SMS message: {0}", locationResponse.Message);
            }

            ServiceResponse<Schedule> scheduleResponse =
                await _scheduleDataService.GetByIdAsync(signUpResponse.Data.ScheduleId);

            if (!scheduleResponse.Success || scheduleResponse.Data == null)
            {
                Log.Error("Error getting Schedule for SMS message: {0}", scheduleResponse.Message);
            }

            StringBuilder sb = new StringBuilder(message.Body, message.Body.Length * 2);
            sb = _mailMergeLogic.ReplaceVolunteerFields(volunteerResponse.Data, scheduleResponse.Data, sb);
            sb = _mailMergeLogic.ReplaceLocationFields(locationResponse.Data, sb);
            sb = _mailMergeLogic.ReplaceScheduleFields(scheduleResponse.Data, sb);
            message.Body = sb.ToString().Trim();
        }
    }
    private async Task<ServiceResponse<bool>> LogSmsMessage(SmsQueue message)
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
        var result = await _smsQueueDataService.UpdateAsync(message);
        if (!result.Success)
            return new ServiceResponse<bool>(result.Message);

        return new ServiceResponse<bool>("SMS Message Logged", true);
    }

    private async Task<ServiceResponse<bool>> SendLiveTextMessage(SmsQueue message)
    {
        bool success = false;
        try
        {
            string accountSid =
                await _configurationDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsAccountSid);

            string authToken =
                await _configurationDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsAuthToken);

            string fromPhone = StringUtil.FilterNumeric(message.FromPhoneNumber);

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
                success = true;
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

        if (success)
            return new ServiceResponse<bool>("SMS Message Sent", true);

        return new ServiceResponse<bool>("SMS Message Failed: " + message.FailureMessage);
    }
}


