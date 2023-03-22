using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface IMediaService // see iMediaDataService
    {   Task<ServiceResponse<Media>> CreateAsync(Media media);
        Task<ServiceResponse<bool>> DeleteAsync(int MediaId);
        Task<ServiceResponse<List<Media>>> GetAllAsync();
        Task<ServiceResponse<Media>> GetAsync(int MediaId);
        Task<ServiceResponse<List<Media>>> GetLogosAsync();
        Task<ServiceResponse<Media>> UpdateAsync(Media media);
        Task<string> MediaLocationPathAsync(Media media);
    } // end interface
} // namespace