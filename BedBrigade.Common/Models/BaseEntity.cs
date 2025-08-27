using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Constants;

namespace BedBrigade.Common.Models
{
    public abstract class BaseEntity
    {
        public DateTime? CreateDate { get; set; }

        [NotMapped]
        public DateTime? CreateDateLocal { get; set; }

        [MaxLength(100)]
        public String? CreateUser { get; set; } = string.Empty;

        public DateTime? UpdateDate { get; set; }

        [NotMapped]
        public DateTime? UpdateDateLocal { get; set; }

        [MaxLength(100)]
        public String? UpdateUser { get; set; } = string.Empty;

        [MaxLength(100)]
        public String? MachineName { get; set; } = string.Empty;

        public void SetCreateAndUpdateUser(String? userName)
        {
            CreateUser = userName ?? Defaults.DefaultUserNameAndEmail;
            CreateDate = DateTime.UtcNow;
            UpdateUser = CreateUser;
            UpdateDate = CreateDate;
            MachineName = Environment.MachineName;
        }

        public void SetUpdateUser(String? userName)
        {
            UpdateUser = userName ?? Defaults.DefaultUserNameAndEmail;
            UpdateDate = DateTime.UtcNow;
            MachineName = Environment.MachineName;
        }
    }
}
