using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("Content")]
    public class Content : BaseEntity
    {
		[Key]
		public Int32 ContentId { get; set; }

		public Location Location { get; set; } = new Location();

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

        public Int32 MainMediaId { get; set; }

		public Int32 LeftMediaId { get; set; }

		public Int32 MiddleMediaId { get; set; }

		public Int32 RightMediaId { get; set; }


	}
}
