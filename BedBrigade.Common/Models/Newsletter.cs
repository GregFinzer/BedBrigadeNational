using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    [Table("Newsletters")]
    public class Newsletter : BaseEntity, ILocationId
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 NewsletterId { get; set; }

        [ForeignKey("LocationId")]
        public Int32 LocationId { get; set; }

        [Required]
        [MaxLength(255)]
        public String Name { get; set; } = string.Empty;
    }
}
