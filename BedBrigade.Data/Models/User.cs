using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;

namespace BedBrigade.Data.Models    

{
    [Table("Users")]
    public class User : BaseEntity
    {
        [Key, MaxLength(50), Required]
        public String UserName { get; set; } = string.Empty;

        [Required]
        public Location Location { get; set; } = new Location();
        [ForeignKey("Location")]
        public int FkLocation { get; set; }
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
        public byte[]? PasswordHash { get; set; }

        [Required]
        [MaxLength(255)]
        public byte[]? PasswordSalt { get; set; }

        [MaxLength(14)]
		public String? Phone { get; set; } = string.Empty;

        public String? Role { get; set; } = string.Empty;

        public string FullName
        { 
            get
            {
                return $"{FirstName} {LastName}";
            }
        }

        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
