using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IVolunteerForService
    {
        Task<ServiceResponse<VolunteerFor>> CreateAsync(VolunteerFor volunteerFor);
        Task<ServiceResponse<bool>> DeleteAsync(int VolunteerForId);
        Task<ServiceResponse<List<VolunteerFor>>> GetAllAsync();
        Task<ServiceResponse<VolunteerFor>> GetAsync(int VolunteerForId);
        Task<ServiceResponse<VolunteerFor>> UpdateAsync(VolunteerFor volunteerFor);
    }
}