using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Data.Entity;
using System.Security.Claims;
using BedBrigade.Common;
using static BedBrigade.Common.Common;


namespace BedBrigade.Data.Services
{
    public class UserDataService : Repository<User>, IUserDataService
    {
        private readonly ICachingService _cachingService;
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly AuthenticationStateProvider _auth;

        public UserDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
            AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
            _auth = authProvider;
        }

        public async Task<ServiceResponse<User>> GetCurrentLoggedInUser()
        {
            return await GetByIdAsync(await GetUserName());
        }

        public async Task<ServiceResponse<List<User>>> GetAllForLocationAsync()
        {
            AuthenticationState authState = await _auth.GetAuthenticationStateAsync();

            Claim? roleClaim = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

            if (roleClaim == null)
                return new ServiceResponse<List<User>>("No Claim of type Role found");
            string roleName = roleClaim.Value;

            Claim? locationClaim = authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId");

            if (locationClaim == null)
                return new ServiceResponse<List<User>>("No Claim of type LocationId found");

            int.TryParse(locationClaim.Value ?? "0", out int locationId);

            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(),
                $"GetAllForLocationAsync with LocationId ({locationId})");
            var cachedContent = _cachingService.Get<List<User>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<User>>(
                    $"Found {cachedContent.Count} {GetEntityName()} records in cache", true, cachedContent);
            ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                if (roleName.ToLower() != RoleNames.NationalAdmin.ToLower())
                {
                    var result = ctx.Users.Where(b => b.LocationId == locationId).ToList();
                    _cachingService.Set(cacheKey, result);
                    return new ServiceResponse<List<User>>($"Found {result.Count()} {GetEntityName()} records", true,
                        result);
                }

                var nationalAdminResponse = ctx.Users.ToList();
                _cachingService.Set(cacheKey, nationalAdminResponse);
                return new ServiceResponse<List<User>>(
                    $"Found {nationalAdminResponse.Count()} {GetEntityName()} records", true, nationalAdminResponse);
            }
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

        public async Task<ServiceResponse<bool>> SaveGridPersistance(Persist persist)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var currentUser = await GetCurrentLoggedInUser();

                if (currentUser.Success && currentUser.Data != null)
                {
                    StorePerstanceData(persist, currentUser.Data);
                    return await SavePersistanceData(context, await GetUserName(), currentUser.Data);
                }

                return new ServiceResponse<bool>($"Unable to find user {GetUserName()}");
            }
        }

        private async Task<ServiceResponse<bool>> SavePersistanceData(DataContext context, string userName, User user)
        {
            try
            {
                var result = context.Users.Update(user);
                await context.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
                return new ServiceResponse<bool>($"Grid Persistance Saved for  {userName}");
            }
            catch (DbException ex)
            {
                return new ServiceResponse<bool>(
                    $"DB error on persist grid record with key {userName} - {ex.Message} ({ex.ErrorCode})");
            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool>($"Error on persist grid record with key {userName} - {ex.Message} ");
            }
        }

        private void StorePerstanceData(Persist persist, User? user)
        {
            switch ((PersistGrid)persist.GridId)
            {
                case PersistGrid.Configuration:
                    user.PersistConfig = persist.UserState;
                    break;
                case PersistGrid.User:
                    user.PersistUser = persist.UserState;
                    break;
                case PersistGrid.Location:
                    user.PersistLocation = persist.UserState;
                    break;
                case PersistGrid.Volunteer:
                    user.PersistVolunteers = persist.UserState;
                    break;
                case PersistGrid.Donation:
                    user.PersistDonation = persist.UserState;
                    break;
                case PersistGrid.Content:
                    //                            user.PersistContent = persist.UserState;
                    break;
                case PersistGrid.BedRequest:
                    user.PersistBedRequest = persist.UserState;
                    break;
                case PersistGrid.Media:
                    user.PersistMedia = persist.UserState;
                    break;
            }
        }

        public async Task<ServiceResponse<string>> GetGridPersistance(Persist persist)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetGridPersistance({persist.GridId}) for user {GetUserName()}");
            var cachedContent = _cachingService.Get<string>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<string>($"Found Grid for id {persist.GridId} in cache", true, cachedContent);

            using (var context = _contextFactory.CreateDbContext())
            {
                string userName = await GetUserName();
                if (userName != null)
                {
                    var user = await context.Users.FindAsync(userName);
                    if (user != null)
                    {
                        var response = new ServiceResponse<string>($"Grid peristance found for user {userName}", true,
                            string.Empty);
                        // 1 = Recipient, 2 = Facility, 3 = Need 4 = Status, 5 = User
                        switch ((PersistGrid)persist.GridId)
                        {
                            case PersistGrid.Configuration:
                                response.Data = user.PersistConfig;
                                break;
                            case PersistGrid.User:
                                response.Data = user.PersistUser;
                                break;
                            case PersistGrid.Location:
                                response.Data = response.Data = user.PersistLocation;
                                break;
                            case PersistGrid.Volunteer:
                                response.Data = user.PersistVolunteers;
                                break;
                            case PersistGrid.Donation:
                                response.Data = user.PersistDonation;
                                break;
                            case PersistGrid.Content:
                                //                            user.PersistContent = persist.UserState;
                                break;
                            case PersistGrid.BedRequest:
                                response.Data = user.PersistBedRequest;
                                break;
                            case PersistGrid.Media:
                                response.Data = user.PersistMedia;
                                break;
                        }

                        _cachingService.Set(cacheKey, response.Data);
                        return response;
                    }
                }

                return new ServiceResponse<string>($"Unable to find user {userName}");
            }
        }
    }
}
