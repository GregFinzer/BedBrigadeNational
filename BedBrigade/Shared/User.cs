using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
    [Table("Users")]
    public class User : BaseEntity
    {
        [Key, MaxLength(50), Required]
        public String UserName { get; set; } = string.Empty;

        [Required]
        public Location Location { get; set; } = new Location();

        [Required]
        [MaxLength(20)]
        public String FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(25)]
        public String LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public String Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public byte[] PasswordHash { get; set; }

        [Required]
        [MaxLength(255)]
        public byte[] PasswordSalt { get; set; }

        [MaxLength(14)]
        public String? Phone { get; set; } = string.Empty;

        public String? Role { get; set; } = string.Empty;

        [MaxLength(5120)]
        public String? PersistConfig { get; set; } = string.Empty;
        [MaxLength(5120)]
        public String? PersistUser { get; set; } = string.Empty;
        [MaxLength(5120)]
        public String? PersistLocation { get; set; } = string.Empty;
        [MaxLength(5120)]
        public String? PersistDonation { get; set; } = string.Empty;
        [MaxLength(5120)]
        public String? PersistPages { get; set; } = string.Empty;
        [MaxLength(5120)]
        public String? PersistVolunteers { get; set; } = string.Empty;





    }
}
