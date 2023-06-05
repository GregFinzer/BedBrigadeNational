using KellermanSoftware.NetCachingLibrary;
using KellermanSoftware.NetCachingLibrary.CacheProviders;

namespace BedBrigade.Data.Services
{
    public class CachingService : ICachingService
    {
        private SmartCache _cache;
        public bool IsCachingEnabled { get; set; }

        public CachingService()
        {
            MemoryCacheProvider provider = new MemoryCacheProvider();
            //TODO:  Load from config
            provider.DefaultExpirationInMinutes = 10;
            SmartConfig config = new SmartConfig(provider);
            config.UserName = LicenseLogic.KellermanUserName;
            config.LicenseKey = LicenseLogic.KellermanLicenseKey;
            _cache = new SmartCache(config);
        }

        public string BuildCacheKey(string section, int key)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("IntegerKey", key);
            return _cache.BuildCacheKey(section, parms);

        }

        public string BuildCacheKey(string section, string key)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("StringKey", key);
            return _cache.BuildCacheKey(section, parms);
        }

        public string BuildCacheKey(string section, int location, string key)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("Location", location);
            parms.Add("StringKey", key);
            return _cache.BuildCacheKey(section, parms);
        }

        public void ClearAll()
        {
            if (!IsCachingEnabled)
            {
                return;
            }

            _cache.ClearAll();
        }

        public void Set<T>(string cacheKey, T value)
        {
            if (!IsCachingEnabled)
            {
                return;
            }

            _cache.Set<T>(cacheKey, value);
        }

        public T? Get<T>(string cacheKey)
        {
            if (!IsCachingEnabled)
            {
                return default(T);
            }

            return _cache.Get<T>(cacheKey);
        }
    }
}
