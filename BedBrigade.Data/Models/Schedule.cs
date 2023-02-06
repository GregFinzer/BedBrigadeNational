using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
	[Table("Schedules")]
	public class Schedule : BaseEntity
    {
		[Key]
		public Int32 ScheduleId { get; set; }

		public Location Location { get; set; } = new Location();

        [Required]
        public DateTime Date { get; set; }
        
        [Required]
		[MaxLength(30)]
		public String ScheduleType { get; set; } = string.Empty;

        [MaxLength(4000)]
        public String? Notes { get; set; } = string.Empty;
    }
}
