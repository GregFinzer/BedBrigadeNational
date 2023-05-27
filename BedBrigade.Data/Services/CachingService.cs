using KellermanSoftware.NetCachingLibrary;
using KellermanSoftware.NetCachingLibrary.CacheProviders;

namespace BedBrigade.Data.Services
{
    public class CachingService : ICachingService
    {
        public SmartCache Cache { get; private set; }

        public CachingService()
        {
            MemoryCacheProvider provider = new MemoryCacheProvider();
            //TODO:  Load from config
            provider.DefaultExpirationInMinutes = 10;
            SmartConfig config = new SmartConfig(provider);
            config.UserName = LicenseLogic.KellermanUserName;
            config.LicenseKey = LicenseLogic.KellermanLicenseKey;
            Cache = new SmartCache(config);
        }

        public string BuildContentCacheKey(int contentId)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("ContentId", contentId);
            return Cache.BuildCacheKey("Content", parms);
        }

        public string BuildContentCacheKey(string name)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("Name", name);
            return Cache.BuildCacheKey("Content", parms);
        }

        public string BuildContentCacheKey(string name, int location)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("Name", name);
            parms.Add("Location", location);
            return Cache.BuildCacheKey("Content", parms);
        }

    }
}
