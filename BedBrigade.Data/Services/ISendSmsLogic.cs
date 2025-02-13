using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services;

public interface ISendSmsLogic
{
    Task<ServiceResponse<bool>> CreateSignUpReminder(SignUp signUp);
    Task<ServiceResponse<bool>> SendTextMessage(int locationId, string phone, string body);
    Task<ServiceResponse<bool>> SendTextMessage(SmsQueue smsQueue);
}
