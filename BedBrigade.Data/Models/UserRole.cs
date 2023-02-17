using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models;

[Table("UserRoles")]
public class UserRole : BaseEntity
{
    [Key]
    public int UserRoleId { get; set; }

    [ForeignKey("LocationId")]
    public int LocationId { get; set; }

    [ForeignKey("UserName1")]
    public string UserName1 { get; set; }

    [ForeignKey("UserName")]
    public string UserName { get; set; }
    [ForeignKey("RoleId")]
    public Int32 RoleId { get; set; } 
}
