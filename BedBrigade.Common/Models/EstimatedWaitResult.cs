namespace BedBrigade.Common.Models;

public class EstimatedWaitResult
{
    public int LocationId { get; set; }
    public int NumberOfWaitingBedRequests { get; set; }
    public DateTime? FirstDeliveryDate { get; set; }
    public DateTime? LastDeliveryDate { get; set; }
    public int NumberOfDeliveredBedRequests { get; set; }
    public double AverageDeliveriesPerDay { get; set; }
    public string EstimatedWait { get; set; } = "Unknown";
}