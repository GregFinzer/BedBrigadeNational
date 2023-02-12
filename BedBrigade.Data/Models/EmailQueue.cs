using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
    [Table("EmailQueue")]
    public class EmailQueue
    {
        [Key]
        public int EmailQueueId;
        [Required, MaxLength(100)]
        public string FromDisplayName { get; set; }
        [Required, MaxLength(100)]
        public string FromAddress { get; set; }
        [Required, MaxLength(100)]
        public string ToAddress { get; set; }
        [Required, MaxLength(100)]
        public string Subject { get; set; }
        [Required, MaxLength(100)]
        public string Body { get; set; }
        [Required, MaxLength(100)]
        public string HtmlBody { get; set; }
        [Required]
        public DateTime QueueDate { get; set; }
        public DateTime LockDate { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime UpdateDate { get; set; }
        [Required]
        public int Priority { get; set; }
        [StringLength(50)]
        public string Status { get; set; }
        [MaxLength(4000)]
        public string FailureMessage { get; set; }
        [MaxLength(100)]
        public String FirstName { get; set; }
    }
}
