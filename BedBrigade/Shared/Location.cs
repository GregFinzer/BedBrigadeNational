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
		public String Name { get; set; }

		[Required]
		[MaxLength(30)]
		public String Route { get; set; }

		[MaxLength(30)]
		public String Address1 { get; set; }

		[MaxLength(30)]
		public String Address2 { get; set; }

		[MaxLength(20)]
		public String City { get; set; }

		[MaxLength(30)]
		public String State { get; set; }

		[Required]
		[MaxLength(10)]
		public String PostalCode { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        public Decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        public Decimal? Longitude { get; set; }


		public ICollection<BedRequest> BedRequests { get; set; }
		public ICollection<ContactUs> ContactUs { get; set; }
		public ICollection<Content> Contents { get; set; }
		public ICollection<Donation> Donations { get; set; }
		public ICollection<Media> Media { get; set; }
		public ICollection<Schedule> Schedules { get; set; }
		public ICollection<User> Users { get; set; }
		public ICollection<Volunteer> Volunteers { get; set; }
	}
}
