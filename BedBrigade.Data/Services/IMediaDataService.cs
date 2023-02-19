using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IMediaDataService
    {
        Task<ServiceResponse<Media>> CreateAsync(Media media);
        Task<ServiceResponse<bool>> DeleteAsync(int MediaId);
        Task<ServiceResponse<List<Media>>> GetAllAsync();
        Task<ServiceResponse<Media>> GetAsync(int MediaId);
        Task<ServiceResponse<Media>> UpdateAsync(Media media);
    }
}