namespace BedBrigade.Common.Models;

/// <summary>
/// Represents a standardized API error response.
/// </summary>
public class ApiError
{
    /// <summary>
    /// A human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
