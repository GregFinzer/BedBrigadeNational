using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models;

public class PaymentSession
{
    /// <summary>
    /// The payment session id is used strictly for verifying when the user has donated
    /// </summary>
    public Guid PaymentSessionId { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "First Name is required")]
    [MaxLength(50)]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Last Name is required")]
    [MaxLength(50)]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Email Address is required")]
    [MaxLength(255)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone Number is required")]
    [MinLength(10, ErrorMessage = "Please enter a valid phone number.")]
    public string? PhoneNumber { get; set; }

    public int? LocationId { get; set; }

    [Required(ErrorMessage = "Campaign is required")]
    public int? DonationCampaignId { get; set; }

    public decimal? DonationAmount { get; set; }
    public decimal? SubscriptionAmount { get; set; }

    public bool IsSaved { get; set; } = false;

    public string? StripeSessionId { get; set; }
}
