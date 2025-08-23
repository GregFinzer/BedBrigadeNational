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
    }
}
