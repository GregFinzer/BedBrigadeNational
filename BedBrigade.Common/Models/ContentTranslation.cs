using BedBrigade.Common.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    [Table("ContentTranslations")]
    public class ContentTranslation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ContentTranslationId { get; set; }

        [ForeignKey("LocationId")]
        public Int32 LocationId { get; set; }

        [Required]
        public ContentType ContentType { get; set; } = ContentType.Body;

        [Required]
        [MaxLength(1024)]
        public String Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Culture is required")]
        [MaxLength(11)]
        public string Culture { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(1024)]
        public String Name { get; set; } = string.Empty;

        //No MaxLength attribute will default to nvarchar(max)
        public String? ContentHtml { get; set; } = string.Empty;
    }
}
