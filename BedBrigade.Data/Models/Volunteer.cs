using BedBrigade.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Models
{
	[Table("Volunteers")]
	public class Volunteer : BaseEntity, ILocationId, IEmail
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 VolunteerId { get; set; }

		[ForeignKey("LocationId")]
		public Int32 LocationId { get; set; }
		       	
		public Boolean IHaveVolunteeredBefore { get; set; }

        [Required(ErrorMessage = "First Name is required.")]
        [MaxLength(20)]        
        public String FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [MaxLength(25)]
		public String LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [Index(IsUnique = true)]
        [EmailInputValidation]
        [MaxLength(255)]
		public String Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required.")]
        [MaxLength(14)]
		public String Phone { get; set; } = string.Empty;

        [MaxLength(80)]
		public String? OrganizationOrGroup { get; set; } = string.Empty;

        [MaxLength(4000)]
		public String? Message { get; set; } = string.Empty;

        public VehicleType VehicleType { get; set; } = VehicleType.NoCar; // default value

        public Boolean IHaveAMinivan { get; set; } = false; // delete

		public Boolean IHaveAnSUV { get; set; } = false; // delete
		
		public Boolean IHaveAPickupTruck { get; set; } = false; // delete

		public int VolunteeringForId { get; set; } = 0; // for compatibility only - should be deleted
		public DateTime VolunteeringForDate { get; set; }// for compatibility only - should be deleted

        public string FullName {
			get
			{
				return $"{FirstName} {LastName}";
			}
		}
	}
}
