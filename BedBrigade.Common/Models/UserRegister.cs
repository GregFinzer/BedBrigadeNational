using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    public class UserRegister
    {
        public User user { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "6 characters minimum.")]
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
