using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IEmailBuilderService
    {
        Task<ServiceResponse<bool>> EmailTaxForms(List<Donation> donations);
        Task<ServiceResponse<bool>> SendBedRequestConfirmationEmail(BedRequest entity);
    }
}
