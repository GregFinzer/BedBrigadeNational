using BedBrigade.Common.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    [Table("ContentHistory")]
    public class ContentHistory : BaseEntity, ILocationId
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ContentHistoryId { get; set; }

        [ForeignKey("LocationId")]
        public Int32 ContentId { get; set; }

        [ForeignKey("LocationId")]
        public Int32 LocationId { get; set; }

        [Required]
        public ContentType ContentType { get; set; } = ContentType.Body;

        [Required]
        [MaxLength(1024)]
        public String Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1024)]
        public String Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? MainImageFileName { get; set; }

        //No MaxLength attribute will default to nvarchar(max)
        public String? ContentHtml { get; set; } = string.Empty;
    }
}
