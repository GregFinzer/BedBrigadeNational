using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services;

public interface ISendSmsLogic
{
    Task<ServiceResponse<bool>> QueueSignUpSmsReminder(SignUp signUp);
    Task<ServiceResponse<bool>> SendTextMessage(int locationId, string phone, string body);
    Task<ServiceResponse<bool>> SendTextMessage(SmsQueue smsQueue);
    Task<ServiceResponse<bool>> QueueDeliverySmsReminder(BedRequest bedRequest, Schedule schedule);
}
