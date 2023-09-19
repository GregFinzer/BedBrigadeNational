using System.ComponentModel.DataAnnotations;
using BedBrigade.Common;

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

        public void SetCreateAndUpdateUser(String? userName)
        {
            CreateUser = userName ?? Constants.DefaultUserNameAndEmail;
            CreateDate = DateTime.UtcNow;
            UpdateUser = CreateUser;
            UpdateDate = CreateDate;
            MachineName = Environment.MachineName;
        }

        public void SetUpdateUser(String? userName)
        {
            UpdateUser = userName ?? Constants.DefaultUserNameAndEmail;
            UpdateDate = DateTime.UtcNow;
            MachineName = Environment.MachineName;
        }
    }
}
