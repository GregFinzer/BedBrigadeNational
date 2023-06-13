using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KellermanSoftware.AddressParser;
using KellermanSoftware.NetCachingLibrary;
using KellermanSoftware.NetCachingLibrary.CacheProviders;
using KellermanSoftware.StaticCodeAnalysis;
using OfficeOpenXml.FormulaParsing.Utilities;

namespace BedBrigade.Common
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
        
    }
}
