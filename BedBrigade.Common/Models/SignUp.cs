using BedBrigade.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
	[Table("SignUps")]
	public class SignUp : BaseEntity, ILocationId
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 SignUpId { get; set; }

        [ForeignKey("LocationId")]
        [Required]
        public Int32 LocationId { get; set; }

        [ForeignKey("ScheduleId")]
		public Int32 ScheduleId { get; set; }
       
		[ForeignKey("VolunteerId")]
        public Int32 VolunteerId { get; set; }
        public Volunteer Volunteer { get; set; }

        public int NumberOfVolunteers { get; set; } = 1;
        public VehicleType VehicleType { get; set; } = VehicleType.None;

        [MaxLength(4000)]
		public String? SignUpNote { get; set; } = string.Empty;

	} // class
} // namespace
