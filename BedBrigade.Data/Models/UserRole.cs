using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
    [Table("UserRoles")]
    public class UserRole : BaseEntity
    {
        [Key]
        public Int32 UserRoleId { get; set; }

        //Parent
        public Location Location { get; set; } = new Location();

        //Parent
        public User User { get; set; } = new User();

        //Parent
        public RoleDb Role { get; set; } = new RoleDb();
    }
}
