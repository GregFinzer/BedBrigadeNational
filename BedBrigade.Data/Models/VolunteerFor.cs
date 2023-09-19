using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
    public class VolunteerFor : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 VolunteerForId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }
    }
}
