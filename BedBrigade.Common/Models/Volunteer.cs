using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;

namespace BedBrigade.Common.Models
{
    [Table("Volunteers")]
	public class Volunteer : BaseEntity, ILocationId, IEmail, IPhone
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 VolunteerId { get; set; }

		[ForeignKey("LocationId")]
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

        [MaxLength(80, ErrorMessage = "Group has a maximum length of 80 characters")]
		public String? OrganizationOrGroup { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of Volunteers is required and must be greater than zero")]
        [Range(1,100)]
        public int NumberOfVolunteers { get; set; } = 1;

        [MaxLength(4000, ErrorMessage = "Message has a maximum length of 4000 characters")]
		public String? Message { get; set; } = string.Empty;

        public VehicleType VehicleType { get; set; } = VehicleType.None; // default value

        public bool AttendChurch { get; set; } = false;

        [MaxLength(4000, ErrorMessage = "Other Languages Spoken has a maximum length of 4000 characters")]
        public string? OtherLanguagesSpoken { get; set;} = string.Empty;

        public bool SubscribedEmail { get; set; } = true;

        public bool SubscribedSms { get; set; } = true;
    
        [NotMapped]
        public string FullName {
			get
			{
                if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
                    return "Unknown";

                return $"{FirstName} {LastName}".Trim();
			}
		}

        [NotMapped]
        public string SearchName
        {
            get
            {
                return $"{LastName}, {FirstName}";
            }
        }

        // additional Schedule/Event fields for SignUpGrid
        [NotMapped]
        public Int32 SignUpId { get; set; } = 0;
        [NotMapped]
		public Int32 ScheduleId { get; set; } = 0;
		[NotMapped]
		public Int32 ScheduleLocationId { get; set; } = 0;
        [NotMapped]
        public String? ScheduleLocationName { get; set; } = string.Empty;
        [NotMapped]
        public String? ScheduleEventName { get; set; } = string.Empty;
		[NotMapped]
		public DateTime? ScheduleEventDate { get; set;}
		[NotMapped]
        public EventType ScheduleEventType { get; set; } = EventType.Delivery;

        [NotMapped]
        public string SignUpGridId
        {
            get
            {
                return Guid.NewGuid().ToString();
            }
        }

    }
}
