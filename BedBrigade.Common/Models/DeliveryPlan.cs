using System;

namespace BedBrigade.Common.Models;

public class DeliveryPlan
{
    public DateTime DeliveryDate { get; set; }
    public string Group { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public int NumberOfBeds { get; set; }
    public int Stops { get; set; }
}
