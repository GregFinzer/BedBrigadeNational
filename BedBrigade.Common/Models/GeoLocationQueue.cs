using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
    [Table("GeoLocationQueue")]
    public class GeoLocationQueue : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeoLocationQueueId { get; set; }

        [MaxLength(40, ErrorMessage = "Street Address has a maximum length of 40 characters")]
        public string Street { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "City has a maximum length of 50 characters")]
        public string City { get; set; }

        [Required]
        [MaxLength(30, ErrorMessage = "State has a maximum length of 30 characters")]
        public string State { get; set; }

        [Required]
        [MaxLength(5, ErrorMessage = "Postal Code has a maximum length of 5 characters")]
        public string PostalCode { get; set; }

        [Required]
        [MaxLength(2, ErrorMessage = "Country Code has a maximum length of 2 characters")]
        public string CountryCode { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Table Name has a maximum length of 50 characters")]
        public string TableName { get; set; }

        [Required]
        public int TableId { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        [Range(-90, 90)]
        public Decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        [Range(-180, 180)]
        public Decimal? Longitude { get; set; }

        [Required]
        public DateTime QueueDate { get; set; }

        public DateTime? LockDate { get; set; }
        public DateTime? ProcessedDate { get; set; }

        [Required]
        public int Priority { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; }

        [MaxLength(4000)]
        public string? FailureMessage { get; set; }
    }
}
