using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Data.Models
{
    [Table("Configurations")]
	public class Configuration : BaseEntity
    {
        [Key, MaxLength(50), Required]
		public String ConfigurationKey { get; set; } = string.Empty;

        [MaxLength(255), Required] 
        public String? ConfigurationValue { get; set; } = string.Empty;


	}
}
