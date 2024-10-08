using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    [Table("TranslationQueue")]
    public class TranslationQueue
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 TranslationQueueId { get; set; }

        [Required(ErrorMessage = "Culture is required")]
        [MaxLength(11)]
        public string Culture { get; set; }

        //No MaxLength attribute will default to nvarchar(max)
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }
    }
}
