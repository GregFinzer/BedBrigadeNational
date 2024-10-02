using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    [Table("ContactUs")]
	public class ContactUs : BaseEntity, ILocationId, IEmail
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ContactUsId { get; set; }

        [ForeignKey("LocationId"), Required]
        public Int32 LocationId { get; set; }

        [Required(ErrorMessage = "First Name is required")]
		[MaxLength(20, ErrorMessage = "First Name has a maximum length of 20 characters")]
		public String FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
		[MaxLength(25, ErrorMessage = "Last Name has a maximum length of 25 characters")]
		public String LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
		[MaxLength(255, ErrorMessage = "Email has a maximum length of 255 characters")]
        public String Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
		[MaxLength(14, ErrorMessage = "Phone has a maximum length of 14 characters")]
        public String Phone { get; set; } = string.Empty;

		[Required(ErrorMessage = "Message is required")]
		[MaxLength(4000, ErrorMessage = "Message has a maximum length of 4000 characters")]
		public String? Message { get; set; } = string.Empty;

        [Required]
        public ContactUsStatus Status { get; set; } = ContactUsStatus.ContactRequested;


    }
}
