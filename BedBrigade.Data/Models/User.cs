using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models;


[Table("Users")]
public class User : BaseEntity
{
    [Key, MaxLength(50), Required(ErrorMessage = "User name is required")]
    public String UserName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a location for the user")]
    [ForeignKey("LocationId")]
    public Int32 LocationId { get; set; }
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(20)]
    public String FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(25)]
    public String LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "An email address is required")]
    [MaxLength(255)]
    public String Email { get; set; } = string.Empty;
    [NotMapped]
    [MinLength(6, ErrorMessage = "Password must be a minimum of 6 characters")]
    public string Password { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public byte[]? PasswordHash { get; set; }

    [MaxLength(255)]
    public byte[]? PasswordSalt { get; set; }

    [MaxLength(14)]
    [Required(ErrorMessage = "Please enter a phone number")]
    public String? Phone { get; set; } = string.Empty;

    public String? Role { get; set; } = string.Empty;
    [ForeignKey("Role")]
    [Range(1,int.MaxValue, ErrorMessage = "Please select a role for the user")]
    public int FkRole { get; set; }

    // Persist the state of the grid component 

    [MaxLength(4000)]
    public string PersistConfig { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string PersistDonation { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string PersistLocation { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string PersistPages { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string PersistUser { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string PersistVolunteers { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string PersistMedia { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string PersistBedRequest { get; set; } = string.Empty;


    public string FullName
    {
        get
        {
            return $"{FirstName} {LastName}";
        }
    }

    public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
