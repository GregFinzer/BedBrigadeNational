using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Email Address is required")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
