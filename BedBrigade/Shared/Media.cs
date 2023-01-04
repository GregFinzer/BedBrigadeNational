using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("Media")]
	public class Media : BaseEntity
    {
		[Key]
		public Int32 MediaId { get; set; }

		public Location Location { get; set; } = new Location();

        [MaxLength(255)] 
        public String? Path { get; set; } = string.Empty;

		[MaxLength(30)] 
        public String? Name { get; set; } = string.Empty;

		[MaxLength(30)]
        public String? MediaType { get; set; } = string.Empty;

		[MaxLength(255)]
		public String? AltText { get; set; } = string.Empty;


	}
}
