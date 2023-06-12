
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Mime;
using ContentType = BedBrigade.Common.Common.ContentType;
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


        public async Task<ServiceResponse<bool>> DeleteAsync(int contentId)
        {
            var result = await _data.DeleteAsync(contentId);
            return result;
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

        public async Task<ServiceResponse<Content>> GetAsync(string name, int locationId)
        {
            return await _data.GetAsync(name, locationId);
        }

        public async Task<ServiceResponse<List<Content>>> GetAllAsync(ContentType type, int locationId)
        {
            return await _data.GetAllAsync(type, locationId);
        }
    }
}
