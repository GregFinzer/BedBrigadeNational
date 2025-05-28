using System;

namespace BedBrigade.Common.Models;

public class PaymentSession
{
    /// <summary>
    /// The payment session id is used strictly for verifying when the user has paid for the appointment.
    /// </summary>
    public Guid PaymentSessionId { get; set; } = Guid.NewGuid();
}
