using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    [Table("Media")]
    public class Media : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 MediaId { get; set; }

        public Int32 LocationId { get; set; }

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
