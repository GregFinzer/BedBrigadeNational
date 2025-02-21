using System.ComponentModel.DataAnnotations;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    public class BulkSmsModel
    {
        [Required]
        public List<Location>? Locations { get; set; }
        public int CurrentLocationId { get; set; }
        public List<EnumNameValue<SmsRecipientOption>> SmsRecipientOptions { get; set; }
        public SmsRecipientOption CurrentSmsRecipientOption { get; set; }
        public List<Schedule>? Schedules { get; set; }
        public int CurrentScheduleId { get; set; }

        [Required(ErrorMessage = "Body is required.")]
        [MaxLength(4000)]
        public string? Body { get; set; }

        public bool ShowEventDropdown { get; set; }

        public bool ShowLocationDropdown { get; set; }
    }
}
