using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
    [Table("Translations")]
    public class Translation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 TranslationId { get; set; }

        public Int32? ParentId { get; set; }

        [Required(ErrorMessage = "Hash is required")]
        [MaxLength(88)]
        public string Hash { get; set; }

        [Required(ErrorMessage = "Culture is required")]
        [MaxLength(11)]
        public string Culture { get; set; } = string.Empty;

        //No MaxLength attribute will default to nvarchar(max)
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = string.Empty;
    }
}
