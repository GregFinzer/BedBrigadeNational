using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class ContentDataService : Repository<Content>, IContentDataService
{
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly AuthenticationStateProvider _auth;


    public ContentDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
    {
        _cachingService = cachingService;
        _contextFactory = contextFactory;
        _auth = authProvider;
    }

    public Task<ServiceResponse<Content>> GetAsync(string name, int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), locationId, name);
        var cachedContent = _cachingService.Get<Content>(cacheKey);

        if (cachedContent != null)
        {
            return Task.FromResult(new ServiceResponse<Content>($"Found {GetEntityName()} in cache", true, cachedContent));
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Content>();
            var result = dbSet.Where(c => c.Name == name && c.LocationId == locationId).FirstOrDefault();

            if (result == null)
            {
                return Task.FromResult(new ServiceResponse<Content>($"Could not find {GetEntityName()} with locationId of {locationId} and a name of {name}", false, null));
            }

            _cachingService.Set(cacheKey, result);
            return Task.FromResult(new ServiceResponse<Content>($"Found {GetEntityName()} with locationId of {locationId} and a name of {name}", true, result));
        }
    }
}


