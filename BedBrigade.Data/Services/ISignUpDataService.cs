using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ISignUpDataService : IRepository<SignUp>
    {
        Task<ServiceResponse<List<SignUp>>> GetAllForLocationAsync(int locationId);
    }
}