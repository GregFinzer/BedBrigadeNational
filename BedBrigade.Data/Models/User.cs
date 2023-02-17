using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models;


[Table("Users")]
public class User : BaseEntity
{
    [Key, MaxLength(50), Required]
    public String UserName { get; set; } = string.Empty;

    [Required]
    [ForeignKey("LocationId")]
    public Int32 LocationId { get; set; }
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
    [ForeignKey("Role")]
    public int FkRole { get; set; }


    public string FullName
    {
        get
        {
            return $"{FirstName} {LastName}";
        }
    }

    public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
