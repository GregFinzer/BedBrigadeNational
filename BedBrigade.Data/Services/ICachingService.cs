namespace BedBrigade.Data.Services
{
    public interface ICachingService
    {
        //Properties
        bool IsCachingEnabled { get; set; }

        //Methods
        string BuildCacheKey(string section, int key);
        string BuildCacheKey(string section, string key);
        string BuildCacheKey(string section, int location, string key);
        void ClearAll();
        void Set<T>(string cacheKey, T value);
        T? Get<T>(string cacheKey);
    }
}
