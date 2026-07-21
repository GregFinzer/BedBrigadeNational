using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;

namespace BedBrigade.Common.Models
{
    [Table("Configurations")]
    public class Configuration : BaseEntity, ILocationId
    {
        private const string MaskedGridValuePlaceholder = "••••••••";
        private string _configurationValue = string.Empty;
        private string? _decryptedValue;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ConfigurationId { get; set; }

        [MaxLength(50), Required]
        public String ConfigurationKey { get; set; } = string.Empty;

        [Required, DefaultValue(1)]
        public int LocationId { get; set; } = Defaults.NationalLocationId;

        [MaxLength(255)]
        public string ConfigurationValue
        {
            get => _configurationValue;
            set
            {
                _configurationValue = value ?? string.Empty;
                _decryptedValue = null;
            }
        }

        [Required, DefaultValue(false)]
        public bool Encrypted { get; set; }

        [NotMapped]
        public string DecryptedValue
        {
            get => _decryptedValue ?? EncryptUtil.DecryptString(ConfigurationValue ?? string.Empty);
            set => _decryptedValue = value;
        }

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

        [NotMapped]
        public bool HasEncryptedValue => Encrypted && EncryptUtil.IsEncrypted(ConfigurationValue ?? string.Empty);

            [NotMapped]
            public string GridDisplayValue => Encrypted
                ? MaskedGridValuePlaceholder
                : ConfigurationValue ?? string.Empty;

        public void PrepareValueForSave()
        {
            string valueToStore = DecryptedValue;
            ConfigurationValue = Encrypted
                ? EncryptUtil.EncryptString(valueToStore)
                : valueToStore;
        }
    }
}
