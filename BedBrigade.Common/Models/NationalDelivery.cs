using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models;

public class NationalDelivery
{
    public string Location { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public int Year { get; set; }

    // Year can be values like "2025 YTD", "2024", "Older", etc.
    public string YearString { get; set; } = string.Empty;

    public int Beds { get; set; }

    // Used for ordering in the UI
    public int SortOrder { get; set; }
}
