using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
	[Table("Donations")]
	public class Donation : BaseEntity, ILocationId
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 DonationId { get; set; }

		[ForeignKey("LocationId"), Required]
		public Int32 LocationId { get; set; }

        [ForeignKey("DonationCampaignId"), Required]
        public Int32 DonationCampaignId { get; set; }

        [Required(ErrorMessage = "Email Address is required")]
        [MaxLength(255)]
        public String? Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public Decimal? TransactionFee { get; set; }

		[MaxLength(255)]
		public String? TransactionId { get; set; } = string.Empty;

		[MaxLength(20)]
		public String? FirstName { get; set; } = string.Empty;

        [MaxLength(25)]
		public String? LastName { get; set; } = string.Empty;

		[Required]
		public DateTime? DonationDate { get; set; }

        [Required]
		public Boolean TaxFormSent { get; set; }

        [MaxLength(80)]
        public string? PaymentProcessor { get; set; }

        [MaxLength(50)]
        public string? PaymentStatus { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public Decimal? Gross { get; set; }

        [MaxLength(50)]
        public string? Currency { get; set; }

        [NotMapped]
		public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string NameAndDate => $"{FullName} - {DonationDate?.ToString("yyyy-MM-dd")}";

        [NotMapped]
        public decimal NetAmount => Gross.HasValue ? Math.Max(0, Gross.Value - TransactionFee ?? 0) : 0;
    }
}
