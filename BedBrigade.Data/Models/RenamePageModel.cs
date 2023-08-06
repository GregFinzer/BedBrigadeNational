using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Data.Models
{
    public class RenamePageModel
    {
        [Required(ErrorMessage = "Page Title is required")]
        public string PageTitle { get; set; }

        [Required(ErrorMessage = "Page Name is required")]
        public string PageName { get; set; }
    }
}
