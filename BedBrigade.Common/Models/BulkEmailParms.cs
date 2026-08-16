namespace BedBrigade.Common.Models
{
    public class BulkEmailParms
    {
        public List<string> EmailList { get; set; } = new List<string>();
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int LocationId { get; set; }
        public int? BedRequestId { get; set; }
        public int? ContactUsId { get; set; }
    }
}
