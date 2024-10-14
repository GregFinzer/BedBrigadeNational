using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    [Table("TranslationQueue")]
    public class TranslationQueue : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 TranslationQueueId { get; set; }

        [Required(ErrorMessage = "Translation ID is required")]
        public Int32 TranslationId { get; set; }

        [Required(ErrorMessage = "Culture is required")]
        [MaxLength(11)]
        public string Culture { get; set; } = string.Empty;
    }
}
