using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Data.Common;

namespace BedBrigade.Data.Services
{
    public class AuthDataService : IAuthDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly IConfiguration _configuration;

        public AuthDataService(IDbContextFactory<DataContext> dbContextFactory, IConfiguration config)
        {
            _contextFactory = dbContextFactory;
            _configuration = config;
        }

        public async Task<ServiceResponse<string>> Login(string email, string password)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                ServiceResponse<string> response = null;
                var user = await context.Users
                    .FirstOrDefaultAsync(x => x.Email.ToLower().Equals(email.ToLower()));
                if (user == null)
                {
                    response = new ServiceResponse<string>("User not found.");
                }
                else if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    response = new ServiceResponse<string>("Wrong password.");
                }
                else
                {
                    response = new ServiceResponse<string>("User logged in", true, await CreateToken(user));
                }

                return response;
            }
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

        private async Task<string> CreateToken(User user)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var location = await context.Locations.FindAsync(user.LocationId);
                List<Claim> claims = new List<Claim>
                {
                new Claim(ClaimTypes.NameIdentifier, user.UserName),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("LocationId", user.LocationId.ToString()),
                new Claim("UserRoute", location.Route)
                };

                var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8
                    .GetBytes(_configuration.GetSection("AppSettings:Token").Value));

                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
                var tokenExpiresIn = await context.Configurations.FirstOrDefaultAsync(c => c.ConfigurationKey == "TokenExpiration");
                int.TryParse(tokenExpiresIn.ConfigurationValue, out int tokenHours);
                var token = new JwtSecurityToken(
                        claims: claims,
                        expires: DateTime.Now.AddHours(tokenHours),
                        signingCredentials: creds);

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);

                return jwt;
            }
        }

        public async Task<ServiceResponse<User>> ChangePassword(string userId, string newPassword)
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
                context.Users.Update(user);
                try
                {
                    await context.SaveChangesAsync();
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
