using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;

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
		[MaxLength(20)]
		public String FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
		[MaxLength(25)]
		public String LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
		[MaxLength(255)]
        [CustomEmailValidation]
        public String Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
		[MaxLength(14)]
        [CustomPhoneValidation]
        public String Phone { get; set; } = string.Empty;

		[Required(ErrorMessage = "Message is required")]
		[MaxLength(4000)]
		public String? Message { get; set; } = string.Empty;

        [Required]
        public ContactUsStatus Status { get; set; } = ContactUsStatus.ContactRequested;


    }
}
