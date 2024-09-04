using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models;


[Table("Users")]
public class User : BaseEntity, ILocationId, IEmail
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


    public string FullName
    {
        get
        {
            return $"{FirstName} {LastName}";
        }
    }

}
