using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using System.Data.Entity;
using System.Security.Claims;
using static BedBrigade.Common.Common;
using EntityState = Microsoft.EntityFrameworkCore.EntityState;

namespace BedBrigade.Data.Services
{
    public class UserDataService : IUserDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly AuthenticationStateProvider _auth;

        protected ClaimsPrincipal _identity;

        public UserDataService(IDbContextFactory<DataContext> dbContextFactory, AuthenticationStateProvider authProvider)
        {
            _contextFactory = dbContextFactory;
            _auth = authProvider;
            Task.Run(() => GetUserClaims(authProvider));
        }

        private async Task GetUserClaims(AuthenticationStateProvider provider)
        {
            var state = await provider.GetAuthenticationStateAsync();
            _identity = state.User;
        }

        public async Task<ServiceResponse<User>> GetAsync(string UserName)
        {
            using (var context = _contextFactory.CreateDbContext())
            {

                var result = await context.Users.FindAsync(UserName);
                if (result != null)
                {
                    return new ServiceResponse<User>("Found Record", true, result);
                }
                return new ServiceResponse<User>("Not Found");
            }
        }

        public async Task<ServiceResponse<List<User>>> GetAllAsync()
        {
            using (var context = _contextFactory.CreateDbContext())
            {

                var authState = await _auth.GetAuthenticationStateAsync();

                var role = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
                List<User> result;
                if (role.ToLower() != "national admin")
                {
                    int.TryParse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0", out int locationId);
                    result = context.Users.Where(u => u.LocationId == locationId).ToList();
                }
                else
                {
                    result = context.Users.ToList();
                }

                if (result != null)
                {
                    return new ServiceResponse<List<User>>($"Found {result.Count} records.", true, result);
                }
                return new ServiceResponse<List<User>>("None found.");
            }
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(string UserName)
        {
            using (var context = _contextFactory.CreateDbContext())
            {

                var user = await context.Users.FindAsync(UserName);
                if (user == null)
                {
                    return new ServiceResponse<bool>($"User record with key {UserName} not found");
                }
                try
                {
                    context.Users.Remove(user);
                    await context.SaveChangesAsync();
                    return new ServiceResponse<bool>($"Removed record with key {UserName}.", true);
                }
                catch (DbException ex)
                {
                    return new ServiceResponse<bool>($"DB error on delete of user record with key {UserName} - {ex.Message} ({ex.ErrorCode})");
                }
            }
        }

        public async Task<ServiceResponse<User>> UpdateAsync(User user)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var entity = await context.Users.FindAsync(user.UserName);

                if (entity != null)
                {
                    context.Entry(entity).CurrentValues.SetValues(user);
                    context.Entry(entity).State = EntityState.Modified;
                    await context.SaveChangesAsync();
                }
                return new ServiceResponse<User>($"User record was updated.",true,user);
            }
            return new ServiceResponse<User>($"User with key {user.UserName} was not updated.");
        }

        private async Task<ServiceResponse<User>> RecordUpdate(User oldRec, User user)
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                try
                {
                    var result = await Task.Run(() => ctx.Users.Update(user));
                    if (result != null)
                    {
                        return new ServiceResponse<User>($"Updated user with key {user.UserName}", true);
                    }

                }
                catch (DbException ex)
                {
                    Log.Logger.Error("Unable to save updated Volunteer record, {0}", ex);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Error while updating Volunteer record, {0}", ex.Message);
                }
            }
            return new ServiceResponse<User>($"User with key {user.UserName} was not updated.");
        }

        public async Task<ServiceResponse<User>> CreateAsync(User user)
            {
                using (var context = _contextFactory.CreateDbContext())
                {

                    try
                    {
                        await context.Users.AddAsync(user);
                        await context.SaveChangesAsync();
                        return new ServiceResponse<User>($"Added user with key {user.UserName}.", true);
                    }
                    catch (DbException ex)
                    {
                        return new ServiceResponse<User>($"DB error on delete of user record with key {user.UserName} - {ex.Message} ({ex.ErrorCode})");
                    }
                }
            }

            public async Task<ServiceResponse<bool>> UserExistsAsync(string email)
            {
                using (var context = _contextFactory.CreateDbContext())
                {

                    var result = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (result != null)
                    {
                        return new ServiceResponse<bool>($"User does exist.", true, true);
                    }
                    return new ServiceResponse<bool>($"User does not exist.", false, false);
                }
            }

            public async Task<ServiceResponse<List<UserRole>>> GetUserRolesAsync()
            {
                using (var context = _contextFactory.CreateDbContext())
                {

                    var result = context.UserRoles.ToList();
                    if (result != null)
                    {
                        return new ServiceResponse<List<UserRole>>("User Roles", true, result);
                    }
                    return new ServiceResponse<List<UserRole>>("No User Roles found.");
                }
            }

            public async Task<ServiceResponse<List<Role>>> GetRolesAsync()
            {
                using (var context = _contextFactory.CreateDbContext())
                {

                    var result = context.Roles.ToList();
                    if (result != null)
                    {
                        return new ServiceResponse<List<Role>>($"Found {result.Count} Roles", true, result);
                    }
                    return new ServiceResponse<List<Role>>("No Roles found.");
                }
            }

            public async Task<ServiceResponse<Role>> GetRoleAsync(int roleId)
            {
                using (var context = _contextFactory.CreateDbContext())
                {

                    var result = await context.Roles.FindAsync(roleId);
                    if (result != null)
                    {
                        return new ServiceResponse<Role>($"Found Role", true, result);
                    }
                    return new ServiceResponse<Role>("No Role found.");
                }
            }

            public async Task<ServiceResponse<bool>> SaveGridPersistance(Persist persist)
            {
                using (var context = _contextFactory.CreateDbContext())
                {
                    var userName = _identity.Claims.FirstOrDefault(t => t.Type == ClaimTypes.NameIdentifier).Value;
                    if (userName != null)
                    {
                        var user = await context.Users.FindAsync(userName);
                        if (user != null)
                        {
                            StorePerstanceData(persist, user);
                            return await SavePersistanceData(context, userName, user);
                        }
                    }

                    return new ServiceResponse<bool>($"Unable to find user {userName}");
                }
            }

            private static async Task<ServiceResponse<bool>> SavePersistanceData(DataContext context, string? userName, User? user)
            {
                try
                {
                    var result = context.Users.Update(user);
                    await context.SaveChangesAsync();
                    return new ServiceResponse<bool>($"Grid Persistance Saved for  {userName}");
                }
                catch (DbException ex)
                {
                    return new ServiceResponse<bool>($"DB error on persist grid record with key {userName} - {ex.Message} ({ex.ErrorCode})");
                }
                catch (Exception ex)
                {
                    return new ServiceResponse<bool>($"Error on persist grid record with key {userName} - {ex.Message} ");
                }
            }

            private static void StorePerstanceData(Persist persist, User? user)
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    var userName = _identity.Claims.FirstOrDefault(t => t.Type == ClaimTypes.NameIdentifier).Value;
                    if (userName != null)
                    {
                        var user = await context.Users.FindAsync(userName);
                        if (user != null)
                        {
                            var response = new ServiceResponse<string>($"Grid peristance found for user {userName}", true, string.Empty);
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
                            return response;
                        }
                    }
                    return new ServiceResponse<string>($"Unable to find user {userName}");
                }
            }
        }
    }
