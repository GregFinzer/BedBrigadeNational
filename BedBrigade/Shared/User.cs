using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
	[Table("Users")]
	public class User : BaseEntity
    {
		[Key, MaxLength(50), Required]
		public String UserName { get; set; }

		[Required]
		public Location Location { get; set; }

		[Required]
		[MaxLength(20)]
		public String FirstName { get; set; }

		[Required]
		[MaxLength(25)]
		public String LastName { get; set; }

		[Required]
		[MaxLength(255)]
		public String Email { get; set; }

		[Required]
		[MaxLength(255)]
		public String PasswordHash { get; set; }

		[MaxLength(14)]
		public String Phone { get; set; }

		public String Role { get; set; }
        

	}
}
