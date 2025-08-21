using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models;

public sealed class ChangePasswordModel
{
    [Required(ErrorMessage = "Password is required")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
        ErrorMessage = "Password must be 8+ chars with upper, lower, number, and special character")]
    [MaxLength(255, ErrorMessage = "Password has a maximum length of 255 characters")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Confirm Password is required")]
    [Compare("Password", ErrorMessage = "The passwords do not match.")]
    public string? ConfirmPassword { get; set; }
}
