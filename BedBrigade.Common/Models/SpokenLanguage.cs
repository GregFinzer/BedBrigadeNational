using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
    [Table("SpokenLanguages")]
    public class SpokenLanguage : BaseEntity
    {
        [Key]
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Value { get; set; } = string.Empty;
    }
}
