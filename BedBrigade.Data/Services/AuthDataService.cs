using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using Serilog;
using System.Data.Common;
using System.Security;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BedBrigade.Data.Services
{
    public class AuthDataService : IAuthDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ILocationDataService _locationDataService;
        private readonly ICachingService _cachingService;
        public AuthDataService(IDbContextFactory<DataContext> dbContextFactory, 
            ILocationDataService locationDataService, ICachingService cachingService)
        {
            _contextFactory = dbContextFactory;
            _locationDataService = locationDataService;
            _cachingService = cachingService;
        }

        public async Task<ServiceResponse<ClaimsPrincipal>> Login(string email, string password)
        {
            if (String.IsNullOrWhiteSpace(email) || String.IsNullOrWhiteSpace(password))
            {
                return new ServiceResponse<ClaimsPrincipal>("Please enter your email and password.");
            }

            using (var context = _contextFactory.CreateDbContext())
            {
                ServiceResponse<ClaimsPrincipal> response = null;
                var user = await context.Users
                    .FirstOrDefaultAsync(x => x.Email.ToLower().Equals(email.ToLower()));
                if (user == null)
                {
                    response = new ServiceResponse<ClaimsPrincipal>("User not found.");
                }
                else if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    response = new ServiceResponse<ClaimsPrincipal>("Wrong password.");
                }
                else
                {
                    var claimsPrincipal = await BuildClaimsPrincipalFromUser(user);
                    response = new ServiceResponse<ClaimsPrincipal>("User logged in", true, claimsPrincipal);
                }

                return response;
            }
        }

        private async Task<ClaimsPrincipal> BuildClaimsPrincipalFromUser(User user)
        {
            var location = await _locationDataService.GetByIdAsync(user.LocationId);

            if (!location.Success) 
            {
                throw new SecurityException($"LocationId {user.LocationId} not found for user {user.Email}.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserName),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("LocationId", user.LocationId.ToString()),
                new Claim("UserRoute", location.Data.Route),
                new Claim("TimeZoneId", location.Data.TimeZoneId),
                new Claim("Phone", (user.Phone ?? string.Empty).FormatPhoneNumber())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            return principal;
        }

        public async Task<ServiceResponse<bool>> Update(UserRegister request)
        {
            using (var context = _contextFactory.CreateDbContext())
            {

                User entity = await context.Users.FindAsync(request.user.UserName);
                if (entity != null)
                {
                    if (!string.IsNullOrEmpty(request.Password))
                    {
                        CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

                        request.user.PasswordHash = passwordHash;
                        request.user.PasswordSalt = passwordSalt;
                    }

                    context.Entry(entity).CurrentValues.SetValues(request.user);
                    context.Entry(entity).State = EntityState.Modified;
                    context.Users.Update(entity);

                }
                else
                {
                    return new ServiceResponse<bool>($"User record not found or password is null or empty, {request.user.UserName} password: {request.Password}", true, true);
                }
                try
                {
                    await context.SaveChangesAsync();
                    _cachingService.ClearByEntityName(nameof(User));
                    return new ServiceResponse<bool>("Updated successfully", true, true);
                }
                catch (DbException ex)
                {
                    Log.Logger.Error("Unable to save updated User record, {0}", ex);
                    return new ServiceResponse<bool>($"Unable to save updated User record {request.user.UserName} - {ex.Message}", true, true);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Error while updating User record, {0}", ex.Message);
                    return new ServiceResponse<bool>($"Error while updating User record, {request.user.UserName} - {ex.Message}", true, true);
                }
            }

        }

        public async Task<ServiceResponse<bool>> Register(User user, string password)
        {
            using (var context = _contextFactory.CreateDbContext())
            {

                if (await UserExists(user.Email))
                {
                    return new ServiceResponse<bool>("User already exists.");
                }

                CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                context.Users.Add(user);
                await context.SaveChangesAsync();
                _cachingService.ClearByEntityName(nameof(User));

                return new ServiceResponse<bool>("Registration successful!", true, true);
            }
        }

        public async Task<bool> UserExists(string email)
        {
            using (var context = _contextFactory.CreateDbContext())
            {

                if (await context.Users.AnyAsync(user => user.Email.ToLower()
                 .Equals(email.ToLower())))
                {
                    return true;
                }
                return false;
            }
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac
                    .ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash =
                    hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }


        public async Task<ServiceResponse<User>> ChangePassword(string userId, string newPassword, bool mustChangePassword)
        {
            using ( var context = _contextFactory.CreateDbContext())
            {

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    return new ServiceResponse<User>("User not found.");
                }

                CreatePasswordHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                user.MustChangePassword = mustChangePassword;
                context.Users.Update(user);
                try
                {
                    await context.SaveChangesAsync();
                    _cachingService.ClearByEntityName(nameof(User));
                }
                catch (Exception ex)
                {
                    return new ServiceResponse<User>($"Error changing password - {ex.Message}");
                }

                return new ServiceResponse<User>("Password has been changed.", true, user);
            }
        }

        public async Task<User> GetUserByEmail(string email)
        {
            using (var context = _contextFactory.CreateDbContext())
            {

                return await context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));
            }
        }
    }
}
