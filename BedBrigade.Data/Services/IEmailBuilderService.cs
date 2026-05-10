using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IEmailBuilderService
    {
        Task<ServiceResponse<bool>> EmailTaxForms(List<Donation> donations);
        Task<ServiceResponse<bool>> SendBedRequestConfirmationEmail(BedRequest entity);
        Task<ServiceResponse<bool>> SendSignUpConfirmationEmail(SignUp signUp, string customMessage);
        Task<ServiceResponse<bool>> SendContactUsConfirmationEmail(ContactUs contactUs);
        Task<ServiceResponse<bool>> SendForgotPasswordEmail(string email, string baseUrl);
        Task<ServiceResponse<bool>> QueueDeliveryEmailReminder(BedRequest bedRequest, Schedule schedule);
        Task<ServiceResponse<bool>> QueueSignUpEmailReminderAsync(SignUp signUp);
    }
}
