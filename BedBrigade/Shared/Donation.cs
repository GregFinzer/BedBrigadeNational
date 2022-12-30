using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("Donations")]
	public class Donation : BaseEntity
    {
		[Key]
		public Int32 DonationId { get; set; }

		public Location Location { get; set; }

		[Required]
		[MaxLength(255)]
		public String Email { get; set; }

		[Required]
		public Decimal Amount { get; set; }

		[MaxLength(255)]
		public String? TransactionId { get; set; }

		[MaxLength(20)]
		public String? FirstName { get; set; }

		[MaxLength(25)]
		public String? LastName { get; set; }

		[Required]
		public Boolean TaxFormSent { get; set; }


	}
}
