using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    public class EmailsToSendParms
    {
        public int LocationId { get; set; }
        public EmailRecipientOption Option { get; set; }
        public int ScheduleId { get; set; }
        public int NewsletterId { get; set; }
    }
}
