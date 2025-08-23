using BedBrigade.Common.Logic;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models;

public sealed class ForgotPasswordModel
{
    [Required(ErrorMessage = "Email Address is required")]
    [MaxLength(255, ErrorMessage = "Email Address has a maximum length of 255 characters")]
    [RegularExpression(Validation.EmailRegexPattern, ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }
}