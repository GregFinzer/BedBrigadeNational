namespace BedBrigade.Common.Models
{
    public class TranslationQueueView
    {
        public string Status { get; set; } = string.Empty;
        public string Culture { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? CreateUser { get; set; } 
        public DateTime QueueDate { get; set; }
        public DateTime QueueDateLocal { get; set; } // new local time
        public DateTime? SentDate { get; set; }
        public DateTime? SentDateLocal { get; set; } // new local time
        public DateTime? LockDate { get; set; }
        public DateTime? LockDateLocal { get; set; } // new local time
        public string? FailureMessage { get; set; }
    }
}