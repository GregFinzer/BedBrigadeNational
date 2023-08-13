using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Data.Models
{
    public abstract class BaseEntity
    {
        public DateTime? CreateDate { get; set; }

        [MaxLength(100)]
        public String? CreateUser { get; set; } = string.Empty;

        public DateTime? UpdateDate { get; set; }

        [MaxLength(100)]
        public String? UpdateUser { get; set; } = string.Empty;

        [MaxLength(100)]
        public String? MachineName { get; set; } = string.Empty;

        public void SetCreateUser(String userName)
        {
            CreateUser = userName;
            CreateDate = DateTime.UtcNow;
            MachineName = Environment.MachineName;
        }

        public void SetUpdateUser(String userName)
        {
            UpdateUser = userName;
            UpdateDate = DateTime.UtcNow;
            MachineName = Environment.MachineName;
        }

        //TODO:  Remove this when all services derive from Repository
        public bool WasUpdatedInTheLastSecond()
        {
            if (UpdateDate == null)
            {
                return false;
            }

            TimeSpan timeSinceLastUpdate = DateTime.UtcNow - UpdateDate.Value;
            return timeSinceLastUpdate.TotalSeconds <= 1;
        }
    }
}
