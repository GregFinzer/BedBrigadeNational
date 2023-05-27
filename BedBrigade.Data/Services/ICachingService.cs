using KellermanSoftware.NetCachingLibrary;

namespace BedBrigade.Data.Services
{
    public interface ICachingService
    {
        //Properties
        SmartCache Cache { get; }

        //Methods
        string BuildContentCacheKey(int contentId);
        string BuildContentCacheKey(string name);
        string BuildContentCacheKey(string name, int location);
    }
}
