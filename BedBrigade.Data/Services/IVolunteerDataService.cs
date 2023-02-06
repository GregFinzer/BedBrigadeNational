using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IVolunteerDataService
    {
        Task<ServiceResponse<Volunteer>> CreateAsync(Volunteer volunteer);
        Task<ServiceResponse<bool>> DeleteAsync(string UserName);
        Task<ServiceResponse<List<Volunteer>>> GetAllAsync();
        Task<ServiceResponse<Volunteer>> GetAsync(int volunteerId);
        Task<ServiceResponse<Volunteer>> UpdateAsync(Volunteer volunteer);
    }
}