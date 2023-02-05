using BedBrigade.Server.Controllers;
using BedBrigade.Server.Data;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;

namespace BedBrigade.Server.Services
{
   public class UserService : IUserService
    {
        private readonly DataContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEnumerable<Claim> _Claims;

        public UserService(DataContext context
            , ILogger<UserController> logger
            , IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _Claims = _httpContextAccessor.HttpContext.User.Claims;

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
            var location = int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "ChurchId").Value);
            var result = _context.Users.Include(l => l.Location).ToList();
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
    }
}
