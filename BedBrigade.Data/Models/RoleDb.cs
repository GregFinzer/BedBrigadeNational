using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
    [Table("Roles")]
    public class RoleDb
    {
        [Key]
        public Int32 RoleId { get; set; }

        [Required]
        [MaxLength(255)]
        public String Name { get; set; } = string.Empty;
    }
}
