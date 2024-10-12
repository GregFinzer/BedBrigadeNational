using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
    [Table("ContentTranslationQueue")]
    public class ContentTranslationQueue : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ContentTranslationQueueId { get; set; }

        [Required(ErrorMessage = "Content ID is required")]
        public Int32 ContentId { get; set; }

        [Required(ErrorMessage = "Culture is required")]
        [MaxLength(11)]
        public string Culture { get; set; } = string.Empty;
    }
}
