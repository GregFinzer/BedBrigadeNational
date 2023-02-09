using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
    [Table("UserRoles")]
    public class UserRole : BaseEntity
    {
        [Key]
        public int UserRoleId { get; set; }

        //Parent
        public Location Location { get; set; } = new Location();

        //Parent
        public User User { get; set; } = new User();

        //Parent
        public Role Role { get; set; } = new Role();
    }
}
