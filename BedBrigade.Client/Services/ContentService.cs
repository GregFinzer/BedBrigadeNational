
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using BedBrigade.Data.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client.Services
{
    public class ContentService : IContentService
    {
        private readonly AuthenticationStateProvider _authState;
        private readonly IContentDataService _data;

        public ContentService(AuthenticationStateProvider authState, IContentDataService dataService)
        {
            _authState = authState;
            _data = dataService;
        }

        /// <summary>
        /// Save a Grids value to persist the contents environment
        /// </summary>
        /// <param name="Content"> The DB column to be saved in the Content table</param>
        /// <returns></returns>
        public async Task<ServiceResponse<string>> GetPersistAsync(Common.PersistGrid Content)
        {
            /// ToDo: Add GetPersist codes
            return new ServiceResponse<string>("Persistance", true, "");
        }
        public async Task<ServiceResponse<bool>> SavePersistAsync(Persist persist)
        {
            /// ToDo: Add SavePersist codes
            return new ServiceResponse<bool>("Saved Persistance data", true);
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int contentId)
        {
            return await _data.DeleteAsync(contentId);
        }

        public async Task<ServiceResponse<Content>> GetAsync(int contentId)
        {
            return await _data.GetAsync(contentId);

        }

        public async Task<ServiceResponse<List<Content>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }

        public async Task<ServiceResponse<Content>> UpdateAsync(Content content)
        {
            return await _data.UpdateAsync(content);
        }

        public async Task<ServiceResponse<Content>> CreateAsync(Content content)
        {
            return await _data.CreateAsync(content);
        }

        public async Task<ServiceResponse<Content>> GetAsync(string name)
        {
            return await _data.GetAsync(name);
        }
    }
}
