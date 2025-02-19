using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Constants;

namespace BedBrigade.Common.Models;

[Table("Locations")]
public class Location : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [DefaultValue(1)]
    public Int32 LocationId { get; set; }

    [Required] [MaxLength(128)] 
    public String Name { get; set; } = string.Empty;

    [Required] [MaxLength(256)] 
    public String Route { get; set; } = string.Empty;

    [MaxLength(256)] 
    public String? MailingAddress { get; set; } = string.Empty;

    [MaxLength(128)] 
    public String? MailingCity { get; set; } = string.Empty;

    [MaxLength(128)] 
    public String? MailingState { get; set; } = string.Empty;

    [MaxLength(10)] 
    public String? MailingPostalCode { get; set; } = string.Empty;

    [MaxLength(256)]
    public String? BuildAddress { get; set; } = string.Empty;

    [MaxLength(128)]
    public String? BuildCity { get; set; } = string.Empty;

    [MaxLength(128)]
    public String? BuildState { get; set; } = string.Empty;

    [MaxLength(10)]
    public String BuildPostalCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,10)")]
    [Range(-90, 90)]
    public Decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(18,10)")]
    [Range(-180, 180)]
    public Decimal? Longitude { get; set; }

    public int? MetroAreaId { get; set; }

    [Column(TypeName = "bit")]
    public bool IsActive { get; set; } = false;

    [MaxLength(40)]
    public string TimeZoneId { get; set;  } 

    public ICollection<BedRequest> BedRequests { get; set; } = new List<BedRequest>();
    public ICollection<ContactUs> ContactUs { get; set; } = new List<ContactUs>();
    public ICollection<Content> Contents { get; set; } = new List<Content>();
    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    public ICollection<Media> Media { get; set; } = new List<Media>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Volunteer> Volunteers { get; set; } = new List<Volunteer>();

    public bool IsMetroLocation()
    {
        return MetroAreaId.HasValue && MetroAreaId > Defaults.MetroAreaNoneId;
    }

}
