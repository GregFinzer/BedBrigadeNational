using BedBrigade.Common.Logic;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Email Address is required")]
        [MaxLength(255, ErrorMessage = "Email Address has a maximum length of 255 characters")]
        [RegularExpression(Validation.EmailRegexPattern, ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255, ErrorMessage = "Password has a maximum length of 255 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
