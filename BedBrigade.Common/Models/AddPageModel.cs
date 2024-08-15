using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Data.Models
{
    public class AddPageModel
    {
        public List<Location> Locations { get; set; }
        public int CurrentLocationId { get; set; }
        public bool IsNationalAdmin { get; set; }
        
        [Required(ErrorMessage = "Page Name is required")]
        public string PageName { get; set; }

        [Required(ErrorMessage = "Page Title is required")]
        public string PageTitle { get; set; }
    }
}
