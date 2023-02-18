using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IVolunteerForDataService
    {
        Task<ServiceResponse<VolunteerFor>> CreateAsync(VolunteerFor volunteerFor);
        Task<ServiceResponse<bool>> DeleteAsync(int VolunteerForId);
        Task<ServiceResponse<List<VolunteerFor>>> GetAllAsync();
        Task<ServiceResponse<VolunteerFor>> GetAsync(int volunteerForId);
        Task<ServiceResponse<VolunteerFor>> UpdateAsync(VolunteerFor volunteerFor);
    }
}