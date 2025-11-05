using System.IO;

namespace BedBrigade.Common.Models
{
    public class DeliveryPlanExportResult
    {
        public string FileName { get; set; } = string.Empty;
        public Stream Stream { get; set; } = Stream.Null;
    }
}
