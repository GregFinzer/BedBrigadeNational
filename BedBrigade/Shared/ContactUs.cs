using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("ContactUs")]
	public class ContactUs : BaseEntity
    {
		[Key]
		public Int32 ContactUsId { get; set; }

		public Location Location { get; set; }

		[Required]
		[MaxLength(20)]
		public String FirstName { get; set; }

		[Required]
		[MaxLength(25)]
		public String LastName { get; set; }

		[Required]
		[MaxLength(255)]
		public String Email { get; set; }

		[Required]
		[MaxLength(14)]
		public String Phone { get; set; }

		[Required]
		[MaxLength(4000)]
		public String Message { get; set; }

        [Required]
        [MaxLength(30)]
		public String Status { get; set; }


	}
}
