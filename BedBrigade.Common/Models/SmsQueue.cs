using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Constants;

namespace BedBrigade.Common.Models
{
    [Table("SmsQueue")]
    public class SmsQueue : BaseEntity, ILocationId
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 SmsQueueId { get; set; }

        [Required]
        [StringLength(14)]
        public string FromPhoneNumber { get; set; }

        [Required][StringLength(14)]
        public string ToPhoneNumber { get; set; }

        [Required]
        [StringLength(1600)]
        public string Body { get; set; }

        [Required]
        public DateTime QueueDate { get; set; }

        [Required]
        public DateTime TargetDate { get; set; }

        public DateTime? LockDate { get; set; }
        public DateTime? SentDate { get; set; }

        [NotMapped]
        public DateTime? SentDateLocal { get; set; }

        [Required]
        public int Priority { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; }

        [MaxLength(4000)]
        public string? FailureMessage { get; set; }

        [ForeignKey("SignUpId")]
        public Int32? SignUpId { get; set; }
        public SignUp? SignUp { get; set; }

        [Required, DefaultValue(false)]
        public bool IsReply { get; set; }

        [Required, DefaultValue(false)]
        public bool IsRead { get; set; }

        [Required, DefaultValue(Defaults.GroveCityLocationId)] 
        public Int32 LocationId { get; set; } = Defaults.GroveCityLocationId;

        [Required, StringLength(20)]
        public string ContactType { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string ContactName { get; set; } = string.Empty;





    }
}
