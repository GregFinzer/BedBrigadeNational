namespace BedBrigade.Common.Enums;

/// <summary>
/// Represents the current stage of a bed request.
/// </summary>
public enum BedRequestStatus
{
    /// <summary>The request is waiting to be scheduled.</summary>
    Waiting = 1,

    /// <summary>The request has been scheduled for delivery.</summary>
    Scheduled = 2,

    /// <summary>The requested beds were delivered to the recipient.</summary>
    Delivered = 3,

    /// <summary>The requested beds were given to the recipient without delivery.</summary>
    Given = 4,

    /// <summary>The request was cancelled.</summary>
    Cancelled = 5
}
