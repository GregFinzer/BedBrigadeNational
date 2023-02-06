using System.Security.Cryptography;

namespace BedBrigade.Data.Seeding
{
    public static class SeedRoutines
    {
        /// <summary>
        /// Create a password hash using a given salt
        /// </summary>
        /// <param name="password">Password to be hashed</param>
        /// <param name="passwordHash">An out parameter of the password hashed</param>
        /// <param out name="passwordSalt">An out parameter used to hash the password</param>
        /// <remarks>This routine was lifted from the AuthService.cs and therefore it is DRY on purpose</remarks>
        public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

    }
}
