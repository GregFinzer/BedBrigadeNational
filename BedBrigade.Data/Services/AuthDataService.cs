using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using Serilog;
using System.Data.Common;
using System.Security;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BedBrigade.Data.Services
{
    public class AuthDataService : IAuthDataService
    {
        private const int DefaultMaxLoginAttempts = 3;
        private const int DefaultLockoutMinutes = 30;
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ILocationDataService _locationDataService;
        private readonly ICachingService _cachingService;
        private readonly IConfigurationDataService _configurationDataService;
        public AuthDataService(IDbContextFactory<DataContext> dbContextFactory, 
            ILocationDataService locationDataService,
            ICachingService cachingService,
            IConfigurationDataService configurationDataService)
        {
            _contextFactory = dbContextFactory;
            _locationDataService = locationDataService;
            _cachingService = cachingService;
            _configurationDataService = configurationDataService;
        }

        public async Task<ServiceResponse<ClaimsPrincipal>> Login(string email, string password)
        {
            string normalizedEmail = NormalizeEmail(email);

            if (String.IsNullOrWhiteSpace(email) || String.IsNullOrWhiteSpace(password))
            {
                LogFailedLoginAttempt(normalizedEmail, "Missing email or password.");
                return new ServiceResponse<ClaimsPrincipal>("Please enter your email and password.");
            }

            using (var context = _contextFactory.CreateDbContext())
            {
                try
                {
                    var user = await GetUserByEmailAsync(context, email);
                    if (user == null)
                    {
                        LogFailedLoginAttempt(normalizedEmail, "User not found.");
                        return new ServiceResponse<ClaimsPrincipal>("User not found.");
                    }

                    var (maxLoginAttempts, lockoutMinutes) = await GetLoginLockoutSettingsAsync();
                    var lockoutResponse = await GetLockoutResponseAsync(context, user);
                    if (lockoutResponse != null)
                    {
                        return lockoutResponse;
                    }

                    if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                    {
                        return await HandleFailedLoginAsync(context, user, maxLoginAttempts, lockoutMinutes);
                    }

                    await ClearLoginFailureStateIfNeededAsync(context, user);
                    var claimsPrincipal = await BuildClaimsPrincipalFromUser(user);
                    Log.Logger.Information("Successful login for {Email} ({UserName})", user.Email, user.UserName);
                    return new ServiceResponse<ClaimsPrincipal>("User logged in", true, claimsPrincipal);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error while logging in user {Email}", email);
                    return new ServiceResponse<ClaimsPrincipal>("There was an error logging in, try again later.");
                }
            }
        }

        private async Task<User?> GetUserByEmailAsync(DataContext context, string email)
        {
            string normalizedEmail = NormalizeEmail(email);
            return await context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);
        }

        private async Task<(int MaxLoginAttempts, int LockoutMinutes)> GetLoginLockoutSettingsAsync()
        {
            try
            {
                int maxLoginAttempts = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                    ConfigNames.MaxLoginAttempts);
                int lockoutMinutes = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                    ConfigNames.LockoutMinutes);

                return (Math.Max(1, maxLoginAttempts), Math.Max(1, lockoutMinutes));
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex,
                    "Unable to load login lockout configuration. Falling back to defaults MaxLoginAttempts={MaxLoginAttempts}, LockoutMinutes={LockoutMinutes}",
                    DefaultMaxLoginAttempts,
                    DefaultLockoutMinutes);

                return (DefaultMaxLoginAttempts, DefaultLockoutMinutes);
            }
        }

        private async Task<ServiceResponse<ClaimsPrincipal>?> GetLockoutResponseAsync(DataContext context, User user)
        {
            if (!user.LockoutEndUtc.HasValue)
            {
                return null;
            }

            if (user.LockoutEndUtc.Value <= DateTime.UtcNow)
            {
                await ClearLoginFailureStateAsync(context, user);
                return null;
            }

            Log.Logger.Warning("Locked out login attempt for {Email} ({UserName}). {Message}",
                user.Email,
                user.UserName,
                LoginLockoutLogic.BuildLockoutMessage(user.LockoutEndUtc.Value));
            return new ServiceResponse<ClaimsPrincipal>(LoginLockoutLogic.BuildLockoutMessage(user.LockoutEndUtc.Value));
        }

        private async Task<ServiceResponse<ClaimsPrincipal>> HandleFailedLoginAsync(DataContext context,
            User user,
            int maxLoginAttempts,
            int lockoutMinutes)
        {
            user.FailedLoginAttempts = Math.Max(0, user.FailedLoginAttempts) + 1;

            if (user.FailedLoginAttempts >= maxLoginAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                await SaveUserAsync(context, user);
                Log.Logger.Warning("User {Email} ({UserName}) locked out after {FailedLoginAttempts} failed login attempts.",
                    user.Email,
                    user.UserName,
                    user.FailedLoginAttempts);
                return new ServiceResponse<ClaimsPrincipal>(LoginLockoutLogic.BuildLockoutMessage(user.LockoutEndUtc.Value));
            }

            await SaveUserAsync(context, user);
            LogFailedLoginAttempt(user.Email, $"Wrong password. Failed attempts: {user.FailedLoginAttempts}.");
            return new ServiceResponse<ClaimsPrincipal>("Wrong password.");
        }

        private static string NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        private static void LogFailedLoginAttempt(string? email, string reason)
        {
            Log.Logger.Warning("Unsuccessful login attempt for {Email}. {Reason}", email ?? string.Empty, reason);
        }

        private async Task ClearLoginFailureStateIfNeededAsync(DataContext context, User user)
        {
            if (user.FailedLoginAttempts == 0 && !user.LockoutEndUtc.HasValue)
            {
                return;
            }

            await ClearLoginFailureStateAsync(context, user);
        }

        private async Task ClearLoginFailureStateAsync(DataContext context, User user)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
            await SaveUserAsync(context, user);
        }

        private async Task SaveUserAsync(DataContext context, User user)
        {
            context.Users.Update(user);
            await context.SaveChangesAsync();
            _cachingService.ClearByEntityName(nameof(User));
        }

        private async Task<ClaimsPrincipal> BuildClaimsPrincipalFromUser(User user)
        {
            var location = await _locationDataService.GetByIdAsync(user.LocationId);

            if (!location.Success || location.Data == null) 
            {
                throw new SecurityException($"LocationId {user.LocationId} not found for user {user.Email}.");
            }

            Location locationData = location.Data;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserName),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim("LocationId", user.LocationId.ToString()),
                new Claim("UserRoute", locationData.Route),
                new Claim("TimeZoneId", locationData.TimeZoneId),
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

                User? entity = await context.Users.FindAsync(request.user.UserName);
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

        private bool VerifyPasswordHash(string password, byte[]? passwordHash, byte[]? passwordSalt)
        {
            if (string.IsNullOrWhiteSpace(password) || passwordHash == null || passwordSalt == null)
            {
                return false;
            }

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

        public async Task<User?> GetUserByEmail(string email)
        {
            using (var context = _contextFactory.CreateDbContext())
            {

                return await context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));
            }
        }
    }
}
