using System.ComponentModel.DataAnnotations;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    public class BulkEmailModel
    {
        [Required]
        public List<Location>? Locations { get; set; }
        public int CurrentLocationId { get; set; }
        public List<EnumNameValue<EmailRecipientOption>> EmailRecipientOptions { get; set; }
        public EmailRecipientOption CurrentEmailRecipientOption { get; set; }
        public List<Schedule>? Schedules { get; set; }
        public int CurrentScheduleId { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(100)]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Body is required.")]
        [MaxLength(4000)]
        public string? Body { get; set; }

        public bool ShowEventDropdown { get; set; }

        public bool ShowLocationDropdown { get; set; }
    }
}
