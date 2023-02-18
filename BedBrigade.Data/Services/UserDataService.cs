
using BedBrigade.Common;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
namespace BedBrigade.Data.Services
{
    public class UserDataService : IUserDataService
    {
        private readonly DataContext _context;
        private readonly AuthenticationStateProvider _auth;

        protected ClaimsPrincipal _identity;

        public UserDataService(DataContext context, AuthenticationStateProvider authProvider) 
        {
            _context = context;
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
            var result = await _context.Users.FindAsync(UserName);
            if (result != null)
            {
                return new ServiceResponse<User>("Found Record", true, result);
            }
            return new ServiceResponse<User>("Not Found");
        }

        public async Task<ServiceResponse<List<User>>> GetAllAsync()
        {
            var authState = await _auth.GetAuthenticationStateAsync();

            var role = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
            List<User> result;
            if (role.ToLower() != "national admin")
            {
                int.TryParse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0", out int locationId);
                result = _context.Users.Where(u => u.LocationId == locationId).ToList();
            }
            else
            {
                result = await _context.Users.ToListAsync();
            }

            if (result != null)
            {
                return new ServiceResponse<List<User>>($"Found {result.Count} records.", true, result);
            }
            return new ServiceResponse<List<User>>("None found.");
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(string UserName)
        {
            var user = await _context.Users.FindAsync(UserName);
            if (user == null)
            {
                return new ServiceResponse<bool>($"User record with key {UserName} not found");
            }
            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return new ServiceResponse<bool>($"Removed record with key {UserName}.", true);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<bool>($"DB error on delete of user record with key {UserName} - {ex.Message} ({ex.ErrorCode})");
            }
        }

        public async Task<ServiceResponse<User>> UpdateAsync(User user)
        {
            var oldRec = await _context.Users.FirstOrDefaultAsync(x => x.UserName == user.UserName);
            if(oldRec != null)
            {
                oldRec.LocationId = user.LocationId;
                oldRec.FirstName = user.FirstName;
                oldRec.LastName = user.LastName;
                oldRec.Email = user.Email;
                oldRec.Phone = user.Phone.FormatPhoneNumber();
                oldRec.PasswordHash = user.PasswordHash;
                oldRec.PasswordSalt = user.PasswordHash;
                oldRec.Role = user.Role;
                oldRec.FkRole= user.FkRole;
            }
            var result = _context.Users.Update(oldRec);
            await _context.SaveChangesAsync();
            //var result = await Task.Run(() => _context.Users.Update(user));
            if (result != null)
            {
                return new ServiceResponse<User>($"Updated user with key {user.UserName}", true);
            }
            
            return new ServiceResponse<User>($"User with key {user.UserName} was not updated.");
        }

        public async Task<ServiceResponse<User>> CreateAsync(User user)
        {
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                return new ServiceResponse<User>($"Added user with key {user.UserName}.", true);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<User>($"DB error on delete of user record with key {user.UserName} - {ex.Message} ({ex.ErrorCode})");
            }
        }

        public async Task<ServiceResponse<bool>> UserExistsAsync(string email)
        {
            var result = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (result != null)
            {
                return new ServiceResponse<bool>($"User does exist.", true, true);
            }
            return new ServiceResponse<bool>($"User does not exist.", false, false);
        }

        public async Task<ServiceResponse<List<UserRole>>> GetUserRolesAsync()
        {
            var result = _context.UserRoles.ToList();
            if(result != null)
            {
                return new ServiceResponse<List<UserRole>>("User Roles", true, result);
            }
            return new ServiceResponse<List<UserRole>>("No User Roles found.");
        }

        public async Task<ServiceResponse<List<Role>>> GetRolesAsync()
        {
            var result = _context.Roles.ToList();
            if (result != null)
            {
                return new ServiceResponse<List<Role>>($"Found {result.Count} Roles", true, result);
            }
            return new ServiceResponse<List<Role>>("No Roles found.");
        }

        public async Task<ServiceResponse<Role>> GetRoleAsync(int roleId)
        {
            var result = await _context.Roles.FindAsync(roleId);
            if(result != null)
            {
                return new ServiceResponse<Role>($"Found Role", true, result);
            }
            return new ServiceResponse<Role>("No Role found.");
        }
    }
}
