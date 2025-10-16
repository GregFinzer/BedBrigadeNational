using BedBrigade.Common.Models;


namespace BedBrigade.Data.Services
{
    public interface ISignUpDataService : IRepository<SignUp>
    {
        Task<ServiceResponse<SignUp>> GetByVolunteerEmailAndScheduleId(int volunteerId, int scheduleId);
        Task<ServiceResponse<SignUp>> Unregister(string volunteerEmail, int scheduleId);
        Task<ServiceResponse<List<SignUp>>> GetAllForLocationAsync(int locationId);
        Task<ServiceResponse<List<SignUp>>> GetSignUpsForDashboard(int locationId);
    }
}