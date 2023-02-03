using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("Locations")]
	public class Location : BaseEntity
    {
		[Key]
		public Int32 LocationId { get; set; }

		[Required]
		[MaxLength(30)]
		public String Name { get; set; } = string.Empty;

		[Required]
		[MaxLength(30)]
		public String Route { get; set; } = string.Empty;

		[MaxLength(30)]
		public String? Address1 { get; set; } = string.Empty;

        [MaxLength(30)]
		public String? Address2 { get; set; } = string.Empty;

        [MaxLength(20)]
		public String? City { get; set; } = string.Empty;

        [MaxLength(30)] 
        public String? State { get; set; } = string.Empty;

		[Required]
		[MaxLength(10)]
		public String PostalCode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,10)")]
        public Decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        public Decimal? Longitude { get; set; }


		public ICollection<BedRequest> BedRequests { get; set; } = new List<BedRequest>();
        public ICollection<ContactUs> ContactUs { get; set; } = new List<ContactUs>();
		public ICollection<Content> Contents { get; set; } = new List<Content>();
		public ICollection<Donation> Donations { get; set; } = new List<Donation>();
		public ICollection<Media> Media { get; set; } = new List<Media>();
		public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
		public ICollection<User> Users { get; set; } = new List<User>();
		public ICollection<Volunteer> Volunteers { get; set; } = new List<Volunteer>();
	}
}
