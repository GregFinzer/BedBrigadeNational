using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
	[Table("ContactUs")]
	public class ContactUs : BaseEntity
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ContactUsId { get; set; }

		public Location Location { get; set; } = new Location();

		[Required]
		[MaxLength(20)]
		public String FirstName { get; set; } = string.Empty;

        [Required]
		[MaxLength(25)]
		public String LastName { get; set; } = string.Empty;

        [Required]
		[MaxLength(255)]
		public String Email { get; set; } = string.Empty;

        [Required]
		[MaxLength(14)]
		public String Phone { get; set; } = string.Empty;

		[Required]
		[MaxLength(4000)]
		public String? Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
		public String Status { get; set; } = string.Empty;


	}
}
