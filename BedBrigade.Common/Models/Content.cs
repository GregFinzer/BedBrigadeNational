using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    [Table("Content")]
    public class Content : BaseEntity, ILocationId
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ContentId { get; set; }

		[ForeignKey("LocationId")]
		public Int32 LocationId { get; set; }

		[Required]
		public ContentType ContentType { get; set; } = ContentType.Body;

		[Required]
		[MaxLength(255)]
		public String Title { get; set; } = string.Empty;

		[Required]
		[MaxLength(255)]
		public String Name { get; set; } = string.Empty;

		[MaxLength(255)]
		public string? MainImageFileName { get; set; }

        //No MaxLength attribute will default to nvarchar(max)
        public String? ContentHtml { get; set; } = string.Empty;
    }
}
