using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
    [Table("Configurations")]
	public class Configuration
	{
        [Key, MaxLength(50), Required]
		public String ConfigurationKey { get; set; }

        [MaxLength(255)]
		public String? ConfigurationValue { get; set; }

		public DateTime? CreateDate { get; set; }

        [MaxLength(100)]
		public String? CreateUser { get; set; }

		public DateTime? UpdateDate { get; set; }

        [MaxLength(100)]
		public String? UpdateUser { get; set; }

        [MaxLength(100)]
		public String? MachineName { get; set; }

	}
}
