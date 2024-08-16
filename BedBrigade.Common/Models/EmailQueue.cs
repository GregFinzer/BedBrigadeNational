using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
    [Table("EmailQueue")]
    public class EmailQueue : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 EmailQueueId { get; set; }

        [MaxLength(100)]
        public string? FromDisplayName { get; set; }

        [Required, MaxLength(100)]
        public string FromAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ReplyToAddress { get; set; }

        [Required, MaxLength(100)]
        public string ToAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ToDisplayName { get; set; }

        [Required, MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        public string Body { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? HtmlBody { get; set; }

        [Required]
        public DateTime QueueDate { get; set; }

        public DateTime? LockDate { get; set; }
        public DateTime? SentDate { get; set; }

        [Required]
        public int Priority { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; }

        [MaxLength(4000)]
        public string? FailureMessage { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }
    }
}
