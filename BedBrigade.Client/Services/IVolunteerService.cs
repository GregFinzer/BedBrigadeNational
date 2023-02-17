using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IVolunteerService
    {
        Task<ServiceResponse<Volunteer>> CreateAsync(Volunteer location);
        Task<ServiceResponse<bool>> DeleteAsync(int VolunteerId);
        Task<ServiceResponse<List<Volunteer>>> GetAllAsync();
        Task<ServiceResponse<Volunteer>> GetAsync(int locationId);
        Task<ServiceResponse<Volunteer>> UpdateAsync(Volunteer location);
    }
}