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

		public Location Location { get; set; } = new Location();

        [Required]
		[MaxLength(255)]
		public String Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public Decimal Amount { get; set; }

		[MaxLength(255)]
		public String? TransactionId { get; set; } = string.Empty;

		[MaxLength(20)]
		public String? FirstName { get; set; } = string.Empty;

        [MaxLength(25)]
		public String? LastName { get; set; } = string.Empty;

        [Required]
		public Boolean TaxFormSent { get; set; }


	}
}
