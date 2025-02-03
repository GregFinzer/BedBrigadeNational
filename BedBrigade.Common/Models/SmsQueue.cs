using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Constants;

namespace BedBrigade.Common.Models
{
    [Table("SmsQueue")]
    public class SmsQueue : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 SmsQueueId { get; set; }

        [Required]
        [StringLength(10)]
        public string FromPhoneNumber { get; set; }

        [Required][StringLength(10)]
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
    }
}
