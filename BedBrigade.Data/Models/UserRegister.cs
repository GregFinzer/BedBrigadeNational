using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Data.Models
{
    public class UserRegister
    {
        public User user { get; set; }
        [StringLength(100, MinimumLength = 6)]
        [Required(ErrorMessage = "New password is required")]
        public string Password { get; set; } = string.Empty;
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
