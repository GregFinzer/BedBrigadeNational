using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
    [Table("MetroAreas")]
    public class MetroArea : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [DefaultValue(1)]
        public Int32 MetroAreaId { get; set; }

        [Required]
        [MaxLength(128)]
        public String Name { get; set; } = string.Empty;
    }
}
