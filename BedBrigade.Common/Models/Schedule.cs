using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    [Table("Schedules")]
    public class Schedule : BaseEntity, ILocationId
    {
        // identification and relationships (2)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]                
        public Int32 ScheduleId { get; set; }
        [ForeignKey("LocationId")]
        [Required]
        public Int32 LocationId { get; set; }
        // Event Description (3)
        [Required, MaxLength(50)]
        public string? EventName { get; set; } = string.Empty;
          [MaxLength(4000)]
        public String? EventNote { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? GroupName { get; set; } = string.Empty;
        // Event Type & Status (2)
        public EventType EventType { get; set; } = EventType.Delivery; // default value
        public EventStatus EventStatus { get; set; } = EventStatus.Scheduled;
        
        [Required]
        public DateTime EventDateScheduled { get; set; }

        public int EventDurationHours { get; set; } = 0;

        // Event Resources  (3)
        
        public int VolunteersMax { get; set; } = 0; 
        public int VolunteersRegistered { get; set; } = 0;
        public int DeliveryVehiclesRegistered { get; set; } = 0;

        [NotMapped]
        public string EventSelect { get; set; }
    }
}

