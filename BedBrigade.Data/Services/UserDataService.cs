
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
            GetUserClaims(authProvider);
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
                result = _context.Users.Where(u => u.FkLocation == locationId).ToList();
            }
            else
            {
                result = _context.Users.ToList();
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
            var result = _context.Users.Update(user);
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
    }
}
