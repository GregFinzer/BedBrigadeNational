using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
    [Table("Subscriptions")]
    public class Subscription : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 SubscriptionId { get; set; }

        [ForeignKey("NewsletterId")]
        public Int32 NewsletterId { get; set; }

        [Required(ErrorMessage = "Email Address is required")]
        [MaxLength(255, ErrorMessage = "Email Address has a maximum length of 255 characters")]
        public String Email { get; set; } = string.Empty;

        public bool Subscribed { get; set; }
    }
}
