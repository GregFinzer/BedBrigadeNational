using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Models
{
    [Table("Media")]
    public class Media : BaseEntity
    {
        [Key]
        public Int32 MediaId { get; set; }
        public Int32 LocationId { get; set; }
        //  public Location Location { get; set; } = new Location(); - creates false locations - commented temporary by VS 2/9/2023

        [MaxLength(260)]
        public String? FilePath { get; set; } = string.Empty; // renamed from Path by VS 2/9/2023

        [MaxLength(255)]
        public String? FileName { get; set; } = string.Empty;
        [MaxLength(30)]
        public String? FileStatus { get; set; } = string.Empty; // added by VS 2/9/2023

        [MaxLength(30)]
        public String? MediaType { get; set; } = string.Empty;

        public int FileSize { get; set; } // modified by VS 2/19/2023

        [MaxLength(255)]
        public String? AltText { get; set; } = string.Empty;

        [Required]
        public FileUse FileUse { get; set; } = FileUse.Unknown;


    }
}
