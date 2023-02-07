
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Cryptography;

namespace BedBrigade.Client.Services
{
    public class UserService : IUserService
    {
        private readonly IUserDataService _data;
        private readonly AuthenticationStateProvider _authState;

        public UserService(AuthenticationStateProvider authState, IUserDataService dataService)
        {
            _data = dataService;
            _authState = authState;
        }

        /// <summary>
        /// Save a Grids value to persist the users environment
        /// </summary>
        /// <param name="User"> The DB column to be saved in the User table</param>
        /// <returns></returns>
        public async Task<ServiceResponse<string>> GetPersistAsync(Common.Common.PersistGrid User)
        {
            /// ToDo: Add GetPersist codes
            return new ServiceResponse<string>("Persistance", true, "");
        }
        public async Task<ServiceResponse<bool>> SavePersistAsync(Persist persist)
        {
            /// ToDo: Add SavePersist codes
            return new ServiceResponse<bool>("Saved Persistance data", true);
        }

        public async Task<ServiceResponse<bool>> DeleteUserAsync(string userName)
        {
            return await _data.DeleteAsync(userName);
        }

        public async Task<ServiceResponse<User>> GetUserAsync(string userName)
        {
            return await _data.GetAsync(userName);
        }

        public async Task<ServiceResponse<List<User>>> GetAllAsync()
        {
            return await _data.GetAllAsync();
        }

        public async Task<ServiceResponse<User>> UpdateAsync(User user)
        {
            return await _data.UpdateAsync(user);
        }

        public async Task<ServiceResponse<User>> CreateAsync(User user)
        {
            return await _data.CreateAsync(user);
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

        public async Task<ServiceResponse<bool>> UserExists(string email)
        {
            return await _data.UserExistsAsync(email);
        }


    }
}
