using BedBrigade.Common.Logic;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    public class UserLogin
    {
        /// <summary>
        /// The email address used for login.
        /// </summary>
        /// <example>someone@somedomain.com</example>
        [Required(ErrorMessage = "Email Address is required")]
        [MaxLength(255, ErrorMessage = "Email Address has a maximum length of 255 characters")]
        [RegularExpression(Validation.EmailRegexPattern, ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The password used for login.
        /// </summary>
        /// <example>secret</example>
        [Required]
        [MaxLength(255, ErrorMessage = "Password has a maximum length of 255 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
