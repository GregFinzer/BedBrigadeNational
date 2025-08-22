using System.ComponentModel.DataAnnotations;
using BedBrigade.Common.Logic;

namespace BedBrigade.Common.Models;

public sealed class ChangePasswordModel
{
    [Required(ErrorMessage = "Password is required")]
    [RegularExpression(Validation.PasswordRegexPattern,
        ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
    [MaxLength(255, ErrorMessage = "Password has a maximum length of 255 characters")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Confirm Password is required")]
    [Compare("Password", ErrorMessage = "The passwords do not match.")]
    public string? ConfirmPassword { get; set; }
}
