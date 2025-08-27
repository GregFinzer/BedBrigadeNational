using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;

namespace BedBrigade.Common.Models;

[Table("BedRequests")]
public class BedRequest : BaseEntity, ILocationId, IEmail, IPhone
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int32 BedRequestId { get; set; }

    [ForeignKey("LocationId"), Required] public Int32 LocationId { get; set; }

    [ForeignKey("ScheduleId")] public Int32? ScheduleId { get; set; }

    [Required(ErrorMessage = "First Name is required")]
    [MaxLength(20, ErrorMessage = "First Name has a maximum length of 20 characters")]
    public String? FirstName { get; set; } = string.Empty;
    
    [MaxLength(25, ErrorMessage = "Last Name has a maximum length of 25 characters")]
    public String? LastName { get; set; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Email Address has a maximum length of 255 characters")]
    public String? Email { get; set; } = string.Empty;

    [MaxLength(14, ErrorMessage = "Phone Number has a maximum length of 14 characters")]
    public String? Phone { get; set; } = string.Empty;

    [NotMapped]
    public String? FormattedPhone
    {
        get { return Phone.FormatPhoneNumber(); }
    }

    [MaxLength(50, ErrorMessage = "Street Address has a maximum length of 50 characters")]
    public String? Street { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "City has a maximum length of 50 characters")] 
    public String? City { get; set; } = string.Empty;

    [MaxLength(30, ErrorMessage = "State has a maximum length of 30 characters")] 
    public String? State { get; set; } = string.Empty;

    [MaxLength(5, ErrorMessage = "Postal Code has a maximum length of 5 characters")]
    public String? PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "The number of ordered beds is required and must be > 0")]
    [Range(1, Int32.MaxValue, ErrorMessage = "The number of ordered beds must be greater than 0")]
    public Int32 NumberOfBeds { get; set; }

    [MaxLength(255, ErrorMessage = "Gender/Age has a maximum length of 255 characters")]
    public String? GenderAge { get; set; } = string.Empty;

    [Required] 
    public BedRequestStatus Status { get; set; } = BedRequestStatus.Waiting;

    [NotMapped]
    public string? StatusString
    {
        get { return EnumHelper.GetEnumDescription(Status); }
    }
    
    [MaxLength(50, ErrorMessage = "Team has a maximum length of 50 characters")]
    public String? Team { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [MaxLength(4000, ErrorMessage = "Notes has a maximum length of 4000")]
    public String? Notes { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    [Required(ErrorMessage = "Please select your Primary Language")]
    public string PrimaryLanguage { get; set; } = string.Empty;

    [MaxLength(10)]
    [Column(TypeName = "nvarchar(10)")]
    public string SpeakEnglish { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,10)")]
    [Range(-90, 90)]
    public Decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(18,10)")]
    [Range(-180, 180)]
    public Decimal? Longitude { get; set; }

    [MaxLength(50, ErrorMessage = "Group has a maximum length of 50 characters")]
    public string? Group { get; set; }

    [MaxLength(100, ErrorMessage = "Reference has a maximum length of 100 characters")]
    public string? Reference { get; set; }

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

    public bool Contacted { get; set; } = false;

    [MaxLength(100, ErrorMessage = "Bed Type has a maximum length of 100 characters")]
    public string? BedType { get; set; }

    [MaxLength(255, ErrorMessage = "Names has a maximum length of 255 characters")]
    public string? Names { get; set; }

    [NotMapped]
    public string ContactedYes => Contacted == true ? "Yes" : string.Empty;

    public void UpdateDuplicateFields(BedRequest? bedRequest, string note)
    {
        if (bedRequest == null) return;

        LocationId = bedRequest.LocationId;
        FirstName = bedRequest.FirstName;
        LastName = bedRequest.LastName;
        Email = bedRequest.Email;
        Phone = bedRequest.Phone;
        Street = bedRequest.Street;
        City = bedRequest.City;
        State = bedRequest.State;
        PostalCode = bedRequest.PostalCode;
        NumberOfBeds = bedRequest.NumberOfBeds;
        GenderAge = bedRequest.GenderAge;
        Names = bedRequest.Names;
        Group = bedRequest.Group;
        Notes = Notes + " " + bedRequest.Notes + " " + note;

        //We intentionally do not update these fields:
        //ScheduleId, Status, Team, DeliveryDate, Contacted, SpeakEnglish, PrimaryLanguage, Reference
    }
}
