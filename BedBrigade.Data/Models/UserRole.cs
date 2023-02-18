using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models;

[Table("UserRoles")]
public class UserRole : BaseEntity
{
    [Key]
    public int UserRoleId { get; set; }

    [ForeignKey("LocationId"), Required]
    public int LocationId { get; set; }

    [ForeignKey("UserName"), MaxLength(50), Required]
    public string UserName { get; set; }

    [ForeignKey("RoleId"), Required]
    public Int32 RoleId { get; set; } 
}
