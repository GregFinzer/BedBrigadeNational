namespace BedBrigade.Data.Services
{
    public interface ICachingService
    {
        //Properties
        bool IsCachingEnabled { get; set; }

        //Methods
        string BuildCacheKey(string entityName, int key);
        string BuildCacheKey(string entityName, string key);
        string BuildCacheKey(string entityName, int location, string key);
        string BuildCacheKey(string entityName, int location, string key, string culture);
        void ClearAll();
        void ClearByEntityName(string entityName);
        void Set<T>(string cacheKey, T value);
        T? Get<T>(string cacheKey);
        void ClearScheduleRelated();
    }
}
