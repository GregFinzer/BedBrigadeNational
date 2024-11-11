using BedBrigade.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
    public class NewBedRequest
    {
        public Int32 LocationId { get; set; }

        public Int32? ScheduleId { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [MaxLength(20, ErrorMessage = "First Name has a maximum length of 20 characters")]
        public String FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
        [MaxLength(25, ErrorMessage = "Last Name has a maximum length of 25 characters")]
        public String LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required")]
        [MaxLength(255, ErrorMessage = "Email Address has a maximum length of 255 characters")]
        public String Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [MaxLength(14, ErrorMessage = "Phone Number has a maximum length of 14 characters")]
        public String Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Street Address is required")]
        [MaxLength(40, ErrorMessage = "Street Address has a maximum length of 40 characters")]
        public String Street { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [MaxLength(50, ErrorMessage = "City has a maximum length of 50 characters")] 
        public String City { get; set; } = string.Empty;

        [MaxLength(30, ErrorMessage = "State has a maximum length of 30 characters")] 
        public String State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal Code (Zip Code) is required")]
        [MaxLength(5, ErrorMessage = "Postal Code has a maximum length of 5 characters")]
        public String PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "The number of ordered beds is required and must be > 0")]
        public Int32 NumberOfBeds { get; set; }

        [Required(ErrorMessage = "Please indicate the age and gender of children")]
        [MaxLength(255, ErrorMessage = "Ages/Gender has a maximum length of 255 characters")]
        public String AgesGender { get; set; } = string.Empty;

        [MaxLength(4000, ErrorMessage = "Special Instructions has a maximum length of 4000 characters")] 
        public String? SpecialInstructions { get; set; } = string.Empty;

        [Required] public BedRequestStatus Status { get; set; } = BedRequestStatus.Waiting;

        [Required(ErrorMessage = "Please select your Primary Language")]       
        public string PrimaryLanguage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your English ability level")]             
        public string SpeakEnglish { get; set; } = string.Empty;
        

    }
}
