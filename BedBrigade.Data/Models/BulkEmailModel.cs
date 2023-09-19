using System.ComponentModel.DataAnnotations;
using BedBrigade.Common;

namespace BedBrigade.Data.Models
{
    public class BulkEmailModel
    {
        [Required]
        public List<Location>? Locations { get; set; }
        public int CurrentLocationId { get; set; }
        public bool IsNationalAdmin { get; set; }
        public List<EnumNameValue<EmailRecipientOption>> EmailRecipientOptions { get; set; }
        public EmailRecipientOption CurrentEmailRecipientOption { get; set; }
        public List<Schedule>? Schedules { get; set; }
        public int CurrentScheduleId { get; set; }

        [Required]
        public string? Subject { get; set; }

        [Required]
        public string? Body { get; set; }

        public bool ShowEventDropdown { get; set; }
    }
}
