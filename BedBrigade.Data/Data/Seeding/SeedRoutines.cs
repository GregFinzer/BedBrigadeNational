using BedBrigade.Data.Data.Seeding;
using System.Security.Cryptography;
using BedBrigade.Common.Models;

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

        public static void SetMaintFields<T>(List<T> entities) where T : BaseEntity
        {
            foreach (var entity in entities)
            {
                SetMaintFields(entity);
            }
        }

        public static void SetMaintFields<T>(T entity) where T : BaseEntity
        {
            entity.CreateUser = SeedConstants.SeedUserName;
            entity.CreateDate = DateTime.UtcNow;
            entity.UpdateUser = SeedConstants.SeedUserName;
            entity.UpdateDate = DateTime.UtcNow;
            entity.MachineName = Environment.MachineName;
        }
    }
}
