
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client.Services
{
    public class MediaService : IMediaService
    {
        private readonly AuthenticationStateProvider _authState;
        private readonly IMediaDataService _data;

        public MediaService(AuthenticationStateProvider authState, IMediaDataService dataService)
        {
            _authState = authState;
            _data = dataService;
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int mediaId) // delete media by ID
        {
            return await _data.DeleteAsync(mediaId);
        } // delete media

        public async Task<ServiceResponse<Media>> GetAsync(int mediaId) // get single media record by ID
        {
            return await _data.GetAsync(mediaId);
        } // get singl;e media record

        public async Task<ServiceResponse<List<Media>>> GetAllAsync() // get all media records
        {
            return await _data.GetAllAsync();
        } // get all media records

        public async Task<ServiceResponse<Media>> UpdateAsync(Media media) // update media record (object)
        {
            return await _data.UpdateAsync(media);
        } // update media record

        public async Task<ServiceResponse<Media>> CreateAsync(Media media) // add new media
        {
            return await _data.CreateAsync(media);
        } // add new media
    } //end MediaService class
} // namespace
