using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
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
        @@@HERE Add Error Handling
        ServiceResponse<Content> templateResult = await _contentDataService.GetByLocationAndContentType(signUp.LocationId, ContentType.SignUpSmsConfirmationForm);
        var fromPhone = await _configurationDataService.GetConfigValueAsync(ConfigSection.Sms, ConfigNames.SmsPhone, signUp.LocationId);

        ServiceResponse<Volunteer> volunteerResult = await _volunteerDataService.GetByIdAsync(signUp.VolunteerId);
        ServiceResponse<Schedule> scheduleResult = await _scheduleDataService.GetByIdAsync(signUp.ScheduleId);

        SmsQueue smsQueue = new SmsQueue()
        {
            SignUpId = signUp.SignUpId,
            FromPhoneNumber = fromPhone,
            ToPhoneNumber = volunteerResult.Data.Phone,
            Body = templateResult.Data.ContentHtml,
            Priority = 1,
            Status = SmsQueueStatus.Queued.ToString(),
            QueueDate = DateTime.UtcNow,
            FailureMessage = string.Empty,
            TargetDate = scheduleResult.Data.EventDateScheduled.AddHours(-2)
        };

        await _smsQueueDataService.CreateAsync(smsQueue);
    }
}


