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

    [Required(ErrorMessage = "First Name is required.")]
    [MaxLength(20)]
    public String? FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last Name is required.")]
    [MaxLength(25)]
    public String LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required.")]
    [MaxLength(255)]
    public String? Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone Number is required.")]
    [MaxLength(14)]
    public String? Phone { get; set; } = string.Empty;

    [NotMapped]
    public String? FormattedPhone
    {
        get { return Phone.FormatPhoneNumber(); }
    }

    [Required(ErrorMessage = "Street Address is required.")]
    [MaxLength(40)]
    public String? Street { get; set; } = string.Empty;

    [Required] [MaxLength(20)] public String? City { get; set; } = string.Empty;

    //[Required]
    [MaxLength(30)] public String? State { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postal Code (Zip Code) is required.")]
    [MaxLength(5)]
    public String? PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "The number of ordered beds is required and must be >0.")]
    public Int32 NumberOfBeds { get; set; }

    [Required(ErrorMessage = "Please indicate the age and gender of children.")]
    [MaxLength(255)]
    public String? AgesGender { get; set; } = string.Empty;

    [MaxLength(4000)] public String? SpecialInstructions { get; set; } = string.Empty;

    [Required] public BedRequestStatus Status { get; set; } = BedRequestStatus.Waiting;

    [NotMapped]
    public string? StatusString
    {
        get { return EnumHelper.GetEnumDescription(Status); }
    }

    public Int32? TeamNumber { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [MaxLength(255)] public String? Notes { get; set; } = string.Empty;

    [NotMapped]
    public string FullName
    {
        get { return $"{FirstName} {LastName}"; }
    }



}
