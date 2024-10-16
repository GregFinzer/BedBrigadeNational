using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;

namespace BedBrigade.Common.Models;

[Table("BedRequests")]
public class BedRequest : BaseEntity, ILocationId, IEmail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int32 BedRequestId { get; set; }

    [ForeignKey("LocationId"), Required] public Int32 LocationId { get; set; }

    [ForeignKey("ScheduleId")] public Int32? ScheduleId { get; set; }

    [Required(ErrorMessage = "First Name is required")]
    [MaxLength(20, ErrorMessage = "First Name has a maximum length of 20 characters")]
    public String? FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last Name is required")]
    [MaxLength(25, ErrorMessage = "Last Name has a maximum length of 25 characters")]
    public String LastName { get; set; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Email Address has a maximum length of 255 characters")]
    public String? Email { get; set; } = string.Empty;

    [MaxLength(14, ErrorMessage = "Phone Number has a maximum length of 14 characters")]
    public String? Phone { get; set; } = string.Empty;

    [NotMapped]
    public String? FormattedPhone
    {
        get { return Phone.FormatPhoneNumber(); }
    }

    [MaxLength(40, ErrorMessage = "Street Address has a maximum length of 40 characters")]
    public String? Street { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "City has a maximum length of 50 characters")] 
    public String? City { get; set; } = string.Empty;

    [MaxLength(30, ErrorMessage = "State has a maximum length of 30 characters")] 
    public String? State { get; set; } = string.Empty;

    [MaxLength(5, ErrorMessage = "Postal Code has a maximum length of 5 characters")]
    public String? PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Number of Beds is required and must be > 0")]
    public Int32 NumberOfBeds { get; set; }

    [MaxLength(255, ErrorMessage = "Ages/Gender has a maximum length of 255 characters")]
    public String? AgesGender { get; set; } = string.Empty;

    [MaxLength(4000, ErrorMessage = "Special Instructions has a maximum length of 4000 characters")] 
    public String? SpecialInstructions { get; set; } = string.Empty;

    [Required] 
    public BedRequestStatus Status { get; set; } = BedRequestStatus.Waiting;

    [NotMapped]
    public string? StatusString
    {
        get { return EnumHelper.GetEnumDescription(Status); }
    }

    public Int32? TeamNumber { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [MaxLength(4000, ErrorMessage = "Notes has a maximum length of 4000")] 
    public String? Notes { get; set; } = string.Empty;

    [NotMapped]
    public string FullName
    {
        get { return $"{FirstName} {LastName}"; }
    }

    /// <summary>
    /// This is used for ordering of the Delivery Sheet
    /// </summary>
    [NotMapped]
    public double Distance { get; set; }

}
