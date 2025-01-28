

using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ISendSmsLogic
    {
        Task<ServiceResponse<bool>> CreateSignUpReminder(SignUp signUp);
    }
}
