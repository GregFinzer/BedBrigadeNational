using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class UserPersistDataService : Repository<UserPersist>, IUserPersistDataService
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;

        public UserPersistDataService(IDbContextFactory<DataContext> contextFactory, 
            ICachingService cachingService, 
            AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
        }

        private string GetCacheKey(UserPersist persist)
        {
            return $"{persist.UserName}_{persist.Grid}";
        }

        public async Task<ServiceResponse<bool>> SaveGridPersistence(UserPersist persist)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var result = await context.UserPersist.FirstOrDefaultAsync(o => o.UserName == persist.UserName
                                                                     && o.Grid == persist.Grid);

                if (result == null)
                {
                    context.UserPersist.Add(persist);
                }
                else
                {
                    result.Data = persist.Data;
                    context.UserPersist.Update(result);
                }

                await context.SaveChangesAsync();
                string persistKey = GetCacheKey(persist);
                string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), persistKey);
                _cachingService.Set(cacheKey, persist.Data);
                return new ServiceResponse<bool>("Persist data saved", true, true);
            }
        }

        public async Task<ServiceResponse<string>> GetGridPersistence(UserPersist persist)
        {
            string persistKey = GetCacheKey(persist);
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), persistKey);
            var cachedContent = _cachingService.Get<UserPersist>(cacheKey);
            if (cachedContent != null)
                return new ServiceResponse<string>($"Found in cache for {persist.UserName} for Grid { persist.Grid}", true,
                cachedContent.Data);

            using (var context = _contextFactory.CreateDbContext())
            {
                var result = await context.UserPersist.FirstOrDefaultAsync(o => o.UserName == persist.UserName
                                                                     && o.Grid == persist.Grid);

                if (result == null)
                {
                    persist.Data = null;
                    _cachingService.Set(cacheKey, new UserPersist());
                    return new ServiceResponse<string>("No persist data found", false, string.Empty);
                }

                _cachingService.Set(cacheKey, result.Data);
                return new ServiceResponse<string>($"Found persist data for {persist.UserName} for Grid { persist.Grid}", true,
                    result.Data);
            }
        }
    }
}
