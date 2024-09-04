using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;
using BedBrigade.Common.Models;


namespace BedBrigade.Data.Services
{
    public class UserDataService : Repository<User>, IUserDataService
    {
        private readonly ICachingService _cachingService;
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly AuthenticationStateProvider _auth;
        private readonly ICommonService _commonService;
        private readonly ILocationDataService _locationDataService;

        public UserDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
            AuthenticationStateProvider authProvider,
            ICommonService commonService,
            ILocationDataService locationDataService) : base(contextFactory, cachingService, authProvider)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
            _auth = authProvider;
            _commonService = commonService;
            _locationDataService = locationDataService;
        }

        public async Task<ServiceResponse<User>> GetCurrentLoggedInUser()
        {
            return await GetByIdAsync(await GetUserName());
        }

        public async Task<ServiceResponse<List<User>>> GetAllForLocationAsync(int locationId)
        {
            return await _commonService.GetAllForLocationAsync(this, locationId);
        }

        //TODO:  This should be broken out into a separate Role service
        public async Task<ServiceResponse<List<Role>>> GetRolesAsync()
        {
            string cacheKey = _cachingService.BuildCacheKey("Role", $"GetRolesAsync");
            var cachedContent = _cachingService.Get<List<Role>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<Role>>($"Found {cachedContent.Count} Role records in cache", true,
                    cachedContent);
            ;

            using (var context = _contextFactory.CreateDbContext())
            {
                var result = context.Roles.ToList();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Role>>($"Found {result.Count} Roles", true, result);
            }
        }

        //TODO:  This should be broken out into a separate Role service
        public async Task<ServiceResponse<Role>> GetRoleAsync(int roleId)
        {
            string cacheKey = _cachingService.BuildCacheKey("Role", $"GetRoleAsync({roleId})");

            using (var context = _contextFactory.CreateDbContext())
            {
                var result = await context.Roles.FindAsync(roleId);
                if (result != null)
                {
                    _cachingService.Set(cacheKey, result);
                    return new ServiceResponse<Role>($"Found Role", true, result);
                }

                return new ServiceResponse<Role>("No Role found.");
            }
        }

        public async Task<ServiceResponse<List<string>>> GetDistinctEmail()
        {
            return await _commonService.GetDistinctEmail(this);
        }

        public async Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId)
        {
            return await _commonService.GetDistinctEmailByLocation(this, locationId);
        }

        public async Task<ServiceResponse<string>> GetEmailSignature(string userName)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetEmailSignature({userName})");
            var cachedContent = _cachingService.Get<string>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<string>($"Found Email Signature for id {userName} in cache", true, cachedContent);

            var user = await GetByIdAsync(userName);
            var location = await _locationDataService.GetByIdAsync(user.Data.LocationId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Thank you,");
            sb.AppendLine($"{user.Data.FirstName} {user.Data.LastName}, {user.Data.Role} {location.Data.Name}");
            sb.AppendLine($"{user.Data.Email}");
            sb.AppendLine($"{user.Data.Phone}");
            var result = sb.ToString();
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<string>($"Email Signature for {user.Data.FirstName} {user.Data.LastName}", true, result);
        }
    }
}
