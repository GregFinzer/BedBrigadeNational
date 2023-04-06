using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
	[Table("Content")]
    public class Content : BaseEntity
    {
		[Key]
		public Int32 ContentId { get; set; }

		[ForeignKey("LocationId")]
		public Int32 LocationId { get; set; } 

        [Required]
		[MaxLength(30)]
		public String ContentType { get; set; } = string.Empty;

		[Required]
		[MaxLength(30)]
		public String Title { get; set; } = string.Empty;

		[Required]
		[MaxLength(30)]
		public String Name { get; set; } = string.Empty;

		public String? ContentHtml { get; set; } = string.Empty;

		// Used to hold HTML element ID for rotator

        public String? HeaderMediaId { get; set; } // holds image for who we are banner

		public String? FooterMediaId { get; set; } // holds image for footer banner

		public String? LeftMediaId { get; set; }

		public String? MiddleMediaId { get; set; }

		public String? RightMediaId { get; set; }


	}
}
