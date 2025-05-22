using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    [Table("DonationCampaigns")]
    public class DonationCampaign : BaseEntity, ILocationId
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 DonationCampaignId { get; set; }

        [ForeignKey("LocationId"), Required]
        public Int32 LocationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CampaignName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

    }
}
