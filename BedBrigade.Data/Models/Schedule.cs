using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
    [Table("Schedules")]
    public class Schedule : BaseEntity
    {
        // identification ans relationships (2)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]                
        public Int32 ScheduleId { get; set; }
        [ForeignKey("LocationId")]
        public Int32 LocationId { get; set; }
        // Event Description (3)
        [Required, MaxLength(50)]
        public string? EventName { get; set; } = string.Empty;
          [MaxLength(4000)]
        public String? EventNote { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? GroupName { get; set; } = string.Empty;
        // Event Type & Status (2)
        public Byte EventType { get; set; } = 0;
        public Byte EventStatus { get; set; } = 1;
        // Event Dates (2)
        [Required]
        public DateTime EventDateScheduled { get; set; }
        public DateTime EventDateCompleted { get; set; }
        // Event Resources  (3)          
        public Byte VehiclesDeliveryMax { get; set; } = 0;
        public Byte VehiclesNormalMax { get; set; } = 0;
        public Byte VolunteersMax { get; set; } = 0;
    }
}

