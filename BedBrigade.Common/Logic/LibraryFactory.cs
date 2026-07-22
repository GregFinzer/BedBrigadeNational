using KellermanSoftware.AddressParser;
using KellermanSoftware.NameParser;
using KellermanSoftware.NetCachingLibrary;
using KellermanSoftware.NetCachingLibrary.CacheProviders;
using KellermanSoftware.NetEmailValidation;
using KellermanSoftware.NetEncryptionLibrary;
using KellermanSoftware.StaticCodeAnalysis;
using KellermanSoftware.USPSStandardization;

namespace BedBrigade.Common.Logic
{
    public static class LibraryFactory
    {

        private static readonly Lazy<AddressParser> _addressParser =
            new(() => new AddressParser(
                LicenseLogic.KellermanUserName,
                LicenseLogic.KellermanLicenseKey));

        private static readonly Lazy<QualityLogic> _qualityLogic =
            new(() => new QualityLogic(
                LicenseLogic.KellermanUserName,
                LicenseLogic.KellermanLicenseKey));

        private static readonly Lazy<EmailValidation> _emailValidation =
            new(() => new EmailValidation(
                LicenseLogic.KellermanUserName,
                LicenseLogic.KellermanLicenseKey));

        private static readonly Lazy<Encryption> _encryption =
            new(() => new Encryption(
                LicenseLogic.KellermanUserName,
                LicenseLogic.KellermanLicenseKey));

        private static readonly Lazy<NameParserLogic> _nameParser =
            new(() => new NameParserLogic(
                LicenseLogic.KellermanUserName,
                LicenseLogic.KellermanLicenseKey));

        private static readonly Lazy<StandardizationLogic> _standardizationLogic =
            new(() => new StandardizationLogic(
                LicenseLogic.KellermanUserName,
                LicenseLogic.KellermanLicenseKey));

        public static SmartConfig CreateSmartConfig(BaseCacheProvider primaryCache)
        {
            SmartConfig config = new SmartConfig(primaryCache);
            config.UserName = LicenseLogic.KellermanUserName;
            config.LicenseKey = LicenseLogic.KellermanLicenseKey;
            return config;
        }

        public static AddressParser AddressParser => _addressParser.Value;

        public static QualityLogic QualityLogic => _qualityLogic.Value;

        public static EmailValidation EmailValidation => _emailValidation.Value;

        public static Encryption Encryption => _encryption.Value;

        public static NameParserLogic NameParser => _nameParser.Value;

        public static StandardizationLogic StandardizationLogic => _standardizationLogic.Value;
    }
}
