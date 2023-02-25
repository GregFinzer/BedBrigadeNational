using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Models;

[Table("BedRequests")]
public class BedRequest : BaseEntity
{
	[Key]
	public Int32 BedRequestId { get; set; }

	[ForeignKey("LocationId"), Required]
	public Int32 LocationId { get; set; } 

	[Required]
	[MaxLength(20)]
	public String? FirstName { get; set; } = string.Empty;

	[Required]
	[MaxLength(25)]
	public String LastName { get; set; } = string.Empty;

	[Required]
	[MaxLength(255)]
	public String? Email { get; set; } = string.Empty;

	[Required]
	[MaxLength(14)]
	public String? Phone { get; set; } = string.Empty;

	[Required]
	[MaxLength(40)]
	public String? Street { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)] 
    public String? City { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public String? State { get; set; } = string.Empty;

    [Required]
	[MaxLength(10)]
	public String? PostalCode { get; set; } = string.Empty;

	[Required]
	public Int32 NumberOfBeds { get; set; }

	[Required]
	[MaxLength(255)]
	public String? AgesGender { get; set; } = string.Empty;

	[MaxLength(4000)]
	public String? SpecialInstructions { get; set; } = string.Empty;

	[Required]
    public BedRequestStatus Status { get; set; } = BedRequestStatus.Requested;

	public Int32? TeamNumber { get; set; }

	public DateTime? DeliveryDate { get; set; }

	[MaxLength(255)]
	public String? Notes { get; set; } = string.Empty;

	public string FullName {
		get
		{
			return $"{FirstName} {LastName}";
		}
	}



}
