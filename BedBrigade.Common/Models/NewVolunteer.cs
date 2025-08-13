using BedBrigade.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Models
{
    public class NewVolunteer
    {
        public Int32 LocationId { get; set; }
        public Boolean IHaveVolunteeredBefore { get; set; }

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

        [MaxLength(80, ErrorMessage = "Organization has a maximum length of 80 characters")]
        public String? Organization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of Volunteers is required")]
        [Range(1, 100)]
        public int NumberOfVolunteers { get; set; } = 1;

        [MaxLength(4000, ErrorMessage = "Message has a maximum length of 4000 characters")]
        public String? Message { get; set; } = string.Empty;

        public VehicleType VehicleType { get; set; } = VehicleType.None; // default value

        public bool AttendChurch { get; set; } = false;

        [MaxLength(4000, ErrorMessage = "Other Languages Spoken has a maximum length of 4000 characters")]
        public string? OtherLanguagesSpoken { get; set; } = string.Empty;

        public bool SubscribedEmail { get; set; } = true;

        public bool SubscribedSms { get; set; } = true;

        [MaxLength(80, ErrorMessage = "Group has a maximum length of 80 characters")]
        public String? Group { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessage = "Church Name has a maximum length of 80 characters")]
        public String? ChurchName { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Volunteer Area has a maximum length of 100 characters")]
        public String? VolunteerArea { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Other Area has a maximum length of 100 characters")]
        public String? OtherArea { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Vehicle Description has a maximum length of 100 characters")]
        public String? VehicleDescription { get; set; } = string.Empty;

        public CanYouTranslate? CanYouTranslate { get; set; }

        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
                    return "Unknown";

                return $"{FirstName} {LastName}".Trim();
            }
        }
    }
}
