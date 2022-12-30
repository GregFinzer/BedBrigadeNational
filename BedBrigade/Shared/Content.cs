using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("Content")]
    public class Content : BaseEntity
    {
		[Key]
		public Int32 ContentId { get; set; }

		public Location Location { get; set; }

		[Required]
		[MaxLength(30)]
		public String ContentType { get; set; }

		[Required]
		[MaxLength(30)]
		public String Title { get; set; }

		[Required]
		[MaxLength(30)]
		public String Name { get; set; }

		public String? ContentHtml { get; set; }

		public Int32? MainMediaId { get; set; }

		public Int32? LeftMediaId { get; set; }

		public Int32? MiddleMediaId { get; set; }

		public Int32? RightMediaId { get; set; }


	}
}
