using KellermanSoftware.NetCachingLibrary;
using KellermanSoftware.NetCachingLibrary.CacheProviders;
using System.Text.RegularExpressions;
using System.Text;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;

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
            SmartConfig config = LibraryFactory.CreateSmartConfig(provider);
            _cache = new SmartCache(config);
        }

        public string BuildCacheKey(string entityName, int key)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("IntegerKey", key);
            return _cache.BuildCacheKey(entityName, parms);

        }

        public string BuildCacheKey(string entityName, string key)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("StringKey", key);
            return _cache.BuildCacheKey(entityName, parms);
        }

        public string BuildCacheKey(string entityName, int location, string key)
        {
            Dictionary<string, object> parms = new Dictionary<string, object>();
            parms.Add("Location", location);
            parms.Add("StringKey", key);
            return _cache.BuildCacheKey(entityName, parms);
        }

        public void ClearAll()
        {
            if (!IsCachingEnabled)
            {
                return;
            }

            _cache.ClearAll();
        }

        public void ClearByEntityName(string entityName)
        {
            if (!IsCachingEnabled)
            {
                return;
            }

            //The cache key is in the form of (EntityName|*
            string wildcard = "(" + entityName + "|*";
            Regex regex = WildcardToRegex(wildcard);
            _cache.ClearByRegex(regex);
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
            //Used to test the spinner
            //System.Threading.Thread.Sleep(1000);

            if (!IsCachingEnabled)
            {
                return default(T);
            }

            return _cache.Get<T>(cacheKey);
        }

        public void ClearScheduleRelated()
        {
            if (!IsCachingEnabled)
            {
                return;
            }

            ClearByEntityName(nameof(Volunteer));
            ClearByEntityName(nameof(Schedule));
            ClearByEntityName(nameof(VolunteerEvent));
            ClearByEntityName(nameof(BedRequest));
        }

        private static Regex WildcardToRegex(string wildcard)
        {
            StringBuilder sb = new StringBuilder(wildcard.Length + 8);

            sb.Append("^");

            for (int i = 0; i < wildcard.Length; i++)
            {
                char c = wildcard[i];
                switch (c)
                {
                    case '*':
                        sb.Append(".*");
                        break;
                    case '?':
                        sb.Append(".");
                        break;
                    // Add any character that needs to be escaped in regex to this case group.
                    case '\\':
                    case '.':
                    case '$':
                    case '^':
                    case '{':
                    case '}':
                    case '(':
                    case ')':
                    case '+':
                    case '|':
                    case '[':
                    case ']':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append("$");

            return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
        }
    }
}
