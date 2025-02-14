using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;

namespace BedBrigade.Common.Models
{
    [Table("Configurations")]
    public class Configuration : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ConfigurationId { get; set; }

        [MaxLength(50), Required]
        public String ConfigurationKey { get; set; } = string.Empty;

        [Required, DefaultValue(1)]
        public int LocationId { get; set; } = Defaults.NationalLocationId;

        [MaxLength(255), Required]
        public String? ConfigurationValue { get; set; } = string.Empty;

        /// <summary>
        /// Defines the section that configuration value belongs to. Defaulted to overall system.
        /// The enum is defined in BedBrigade.Common
        /// </summary>
        [Required]
        public ConfigSection Section { get; set; } = ConfigSection.System;

        [NotMapped]
        public string SectionDescription
        {
            get { return EnumHelper.GetEnumDescription(Section); }
        }
    }
}
