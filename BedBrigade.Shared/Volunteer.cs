using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("Volunteers")]
	public class Volunteer : BaseEntity
    {
		[Key]
		public Int32 VolunteerId { get; set; }

		public Location Location { get; set; } = new Location();

        [Required] 
        [MaxLength(30)] 
        public String VolunteeringFor { get; set; } = string.Empty;

		[Required]
		public DateTime VolunteeringForDate { get; set; }

		[Required]
		public Boolean IHaveVolunteeredBefore { get; set; }

		[Required]
		[MaxLength(20)]
		public String FirstName { get; set; } = string.Empty;

        [Required]
		[MaxLength(25)]
		public String LastName { get; set; } = string.Empty;

        [Required]
		[MaxLength(255)]
		public String Email { get; set; } = string.Empty;

        [Required]
		[MaxLength(14)]
		public String Phone { get; set; } = string.Empty;

        [MaxLength(80)]
		public String? OrganizationOrGroup { get; set; } = string.Empty;

        [MaxLength(4000)]
		public String? Message { get; set; } = string.Empty;

        [Required]
		public Boolean IHaveAMinivan { get; set; }

		[Required]
		public Boolean IHaveAnSUV { get; set; }

		[Required]
		public Boolean IHaveAPickupTruck { get; set; }


	}
}
