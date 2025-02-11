namespace BedBrigade.Common.Models
{
    public class SmsQueueSummary
    {
        public int Id { get; set; } = 1;
        public string ContactName { get; set; } = string.Empty;
        public string ToPhoneNumber { get; set; }
        public DateTime? MessageDate { get; set; }
        public string Body { get; set; }
        public string ContactType { get; set; } = string.Empty;
        public int MessageCount { get; set; }

        public bool UnRead { get; set; }
    }
}
