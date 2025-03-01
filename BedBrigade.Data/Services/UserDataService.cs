
using Microsoft.EntityFrameworkCore;
using System.Text;
using BedBrigade.Common.Models;
using System.Data.Common;
using BedBrigade.Common.Constants;
using Twilio.TwiML.Voice;


namespace BedBrigade.Data.Services
{
    public class UserDataService : Repository<User>, IUserDataService
    {
        private readonly ICachingService _cachingService;
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly IAuthService _authService;
        private readonly ICommonService _commonService;
        private readonly ILocationDataService _locationDataService;
        private readonly IUserPersistDataService _userPersistDataService;

        public UserDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
            IAuthService authService,
            ICommonService commonService,
            ILocationDataService locationDataService,
            IUserPersistDataService userPersistDataService) : base(contextFactory, cachingService, authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
            _authService = authService;
            _commonService = commonService;
            _locationDataService = locationDataService;
            _userPersistDataService = userPersistDataService;
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

        public async Task<ServiceResponse<User>> GetByPhone(string phone)
        {
            return await _commonService.GetByPhone(this, phone);
        }

        public override async Task<ServiceResponse<bool>> DeleteAsync(object id)
        {
            var existingUser = await GetByIdAsync(id);

            if (!existingUser.Success || existingUser.Data == null)
                return new ServiceResponse<bool>($"Could not find {GetEntityName()} with id {id}", false); 

            var userDelete = await base.DeleteAsync(id);

            if (!userDelete.Success)
                return userDelete;

            var userPersistDelete = await _userPersistDataService.DeleteByUserName(existingUser.Data.UserName);

            if (!userPersistDelete.Success)
                return userPersistDelete;

            return new ServiceResponse<bool>($"Deleted {GetEntityName()} with id {id} for UserName {existingUser.Data.UserName}", true);
        }

        public async Task<ServiceResponse<List<string>>> GetDistinctPhone()
        {
            return await _commonService.GetDistinctPhone(this);
        }

        public async Task<ServiceResponse<List<string>>> GetDistinctPhoneByLocation(int locationId)
        {
            return await _commonService.GetDistinctPhoneByLocation(this, locationId);
        }

        public async Task<List<string>> GetMissedMessageEmailsForLocation(int locationId)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), locationId, "GetMissedMessageEmailsForLocation()");
            List<string>? cachedContent = _cachingService.Get<List<string>>(cacheKey);

            if (cachedContent != null)
            {
                return cachedContent;
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<User>();
                var result = await dbSet.Where(o => o.LocationId == locationId 
                                                    && !String.IsNullOrEmpty(o.Email) 
                                                    && (o.Role == RoleNames.LocationAdmin
                                                    || o.Role == RoleNames.LocationScheduler
                                                    || o.Role == RoleNames.LocationCommunications))
                    .Select(o => o.Email.ToLower()).Distinct()
                    .ToListAsync();
                _cachingService.Set(cacheKey, result);
                return result;
            }
        }


    }
}
