using KellermanSoftware.AddressParser;
using KellermanSoftware.NameParser;
using KellermanSoftware.NetCachingLibrary;
using KellermanSoftware.NetCachingLibrary.CacheProviders;
using KellermanSoftware.NetEmailValidation;
using KellermanSoftware.NetEncryptionLibrary;
using KellermanSoftware.StaticCodeAnalysis;

namespace BedBrigade.Common.Logic
{
    public static class LibraryFactory
    {
        public static SmartConfig CreateSmartConfig(BaseCacheProvider primaryCache)
        {
            SmartConfig config = new SmartConfig(primaryCache);
            config.UserName = LicenseLogic.KellermanUserName;
            config.LicenseKey = LicenseLogic.KellermanLicenseKey;
            return config;
        }

        public static AddressParser CreateAddressParser()
        {
            AddressParser parser = new AddressParser(LicenseLogic.KellermanUserName, LicenseLogic.KellermanLicenseKey);
            return parser;
        }

        public static QualityLogic CreateQualityLogic()
        {
            QualityLogic logic = new QualityLogic(LicenseLogic.KellermanUserName, LicenseLogic.KellermanLicenseKey);
            return logic;
        }

        public static EmailValidation CreateEmailValidation()
        {
            EmailValidation validation =
                new EmailValidation(LicenseLogic.KellermanUserName, LicenseLogic.KellermanLicenseKey);
            return validation;
        }

        public static Encryption CreateEncryption()
        {
            Encryption encryption = new Encryption(LicenseLogic.KellermanUserName, LicenseLogic.KellermanLicenseKey);
            return encryption;
        }

        public static NameParserLogic CreateNameParser()
        {
            NameParserLogic nameParser = new NameParserLogic(LicenseLogic.KellermanUserName, LicenseLogic.KellermanLicenseKey);
            return nameParser;
        }
    }
}
