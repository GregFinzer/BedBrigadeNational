using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IEmailBuilderService
    {
        Task<ServiceResponse<bool>> EmailTaxForms(List<Donation> donations);
        Task<ServiceResponse<bool>> SendBedRequestConfirmationEmail(BedRequest entity);
        Task<ServiceResponse<bool>> SendSignUpConfirmationEmail(SignUp signUp, string customMessage);
    }
}
