using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("BedRequests")]
	public class BedRequest : BaseEntity
	{
		[Key]
		public Int32 BedRequestId { get; set; }

		//Parent
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
		[MaxLength(40)]
		public String Street { get; set; }

		[Required]
		[MaxLength(20)]
		public String City { get; set; }

		[Required]
		[MaxLength(10)]
		public String PostalCode { get; set; }

		[Required]
		public Int32 NumberOfBeds { get; set; }

		[Required]
		[MaxLength(255)]
		public String AgesGender { get; set; }

		[MaxLength(4000)]
		public String SpecialInstructions { get; set; }

		[Required]
		[MaxLength(30)]
		public String Status { get; set; }

		public Int32? TeamNumber { get; set; }

		public DateTime? DeliveryDate { get; set; }

		[MaxLength(255)]
		public String Notes { get; set; }



	}
}
