using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services;

public class SendSmsLogic : ISendSmsLogic
{
    private readonly IContentDataService _contentDataService;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IVolunteerDataService _volunteerDataService;
    private readonly ISmsQueueDataService _smsQueueDataService;
    private readonly IScheduleDataService _scheduleDataService;

    public SendSmsLogic(IContentDataService contentDataService, 
        IConfigurationDataService configurationDataService, 
        IVolunteerDataService volunteerDataService,
        ISmsQueueDataService smsQueueDataService,
        IScheduleDataService scheduleDataService)
    {
        _contentDataService = contentDataService;
        _configurationDataService = configurationDataService;
        _volunteerDataService = volunteerDataService;
        _smsQueueDataService = smsQueueDataService;
        _scheduleDataService = scheduleDataService;
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
            Priority = 1,
            Status = SmsQueueStatus.Queued.ToString(),
            QueueDate = DateTime.UtcNow,
            FailureMessage = string.Empty,
            TargetDate = scheduleResult.Data.EventDateScheduled.AddHours(-2),
            IsRead = true,
            IsReply = false,
            LocationId = signUp.LocationId,
            ContactType = ContactTypes.Volunteer,
            ContactName = volunteerResult.Data.FullName
        };
        return smsQueue;
    }
}


