using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models;

[Table("UserRoles")]
public class UserRole : BaseEntity
{
    [Key]
    public int UserRoleId { get; set; }

    //Parent
    [ForeignKey("LocationId")]
    public int LocationId { get; set; } 

    //Parent
    public User User { get; set; } = new User();

    //Parent
    public Role Role { get; set; } = new Role();
}
