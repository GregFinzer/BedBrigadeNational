using BedBrigade.Common.Models;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace BedBrigade.Data.Services
{
    public interface ISignUpDataService : IRepository<SignUp>
    {
        Task<ServiceResponse<List<SignUp>>> GetAllForLocationAsync(int locationId);
        Task<ServiceResponse<SignUp>> GetByVolunteerEmailAndScheduleId(int volunteerId, int scheduleId);
    }
}