using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Shared
{
    [Table("Configurations")]
	public class Configuration : BaseEntity
    {
        [Key, MaxLength(50), Required]
		public String ConfigurationKey { get; set; }

        [MaxLength(255)]
		public String ConfigurationValue { get; set; }


	}
}
