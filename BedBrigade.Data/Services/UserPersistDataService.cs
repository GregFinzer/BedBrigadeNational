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
            //Save to the cache immediately in case the user goes back to the grid
            string persistKey = GetCacheKey(persist);
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), persistKey);
            _cachingService.Set(cacheKey, persist);

            try
            {
                using (var context = _contextFactory.CreateDbContext())
                {
                    var result = await context.UserPersist.FirstOrDefaultAsync(o => o.UserName == persist.UserName
                        && o.Grid == persist.Grid);

                    if (result == null)
                    {
                        await base.CreateAsync(persist);
                    }
                    else
                    {
                        await base.UpdateAsync(persist);
                    }

                    return new ServiceResponse<bool>("Persist data saved", true, true);
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool>($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<string>> GetGridPersistence(UserPersist persist)
        {
            try
            {
                string persistKey = GetCacheKey(persist);
                string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), persistKey);
                var cachedContent = _cachingService.Get<UserPersist>(cacheKey);
                if (cachedContent != null)
                {
                    if (String.IsNullOrEmpty(cachedContent.Data))
                    {
                        return new ServiceResponse<string>("No persist data found", false, null);
                    }

                    return new ServiceResponse<string>($"Found in cache for {persist.UserName} for Grid {persist.Grid}",
                        true,
                        cachedContent.Data);
                }

                using (var context = _contextFactory.CreateDbContext())
                {
                    var result = await context.UserPersist.FirstOrDefaultAsync(o => o.UserName == persist.UserName
                        && o.Grid == persist.Grid);

                    if (result == null)
                    {
                        persist.Data = null;
                        _cachingService.Set(cacheKey, new UserPersist());
                        return new ServiceResponse<string>("No persist data found", false, null);
                    }

                    _cachingService.Set(cacheKey, result.Data);
                    return new ServiceResponse<string>(
                        $"Found persist data for {persist.UserName} for Grid {persist.Grid}", true,
                        result.Data);
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> DeleteByUserName(string userName)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var result = await context.UserPersist.Where(o => o.UserName == userName).ToListAsync();
                if (result.Count == 0)
                    return new ServiceResponse<bool>("Nothing to delete for this user", true, true);

                context.UserPersist.RemoveRange(result);
                await context.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
                return new ServiceResponse<bool>("Persist data deleted", true, true);
            }
        }
    }
}
