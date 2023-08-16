using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BedBrigade.Common;

namespace BedBrigade.Data.Services;

public class BedRequestDataService : Repository<BedRequest>, IBedRequestDataService
{
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly AuthenticationStateProvider _auth;

    public BedRequestDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _auth = authProvider;
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetAllForLocationAsync()
    {
        AuthenticationState authState = await _auth.GetAuthenticationStateAsync();

        Claim? roleClaim = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        if (roleClaim == null)
            return new ServiceResponse<List<BedRequest>>("No Claim of type Role found");
        string roleName = roleClaim.Value;

        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetAllAsync with role ({roleName})");
        var cachedContent = _cachingService.Get<List<BedRequest>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<BedRequest>>($"Found {cachedContent.Count} BedRequests records in cache", true, cachedContent); ;
        
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<BedRequest>();
            if (roleName.ToLower() != RoleNames.NationalAdmin.ToLower())
            {
                int.TryParse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0",
                    out int locationId);
                var result = await dbSet.Where(b => b.LocationId == locationId).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<BedRequest>>($"Found {result.Count()} BedRequests", true, result);
            }

            var nationalAdminResponse = await dbSet.ToListAsync();
            _cachingService.Set(cacheKey, nationalAdminResponse);
            return new ServiceResponse<List<BedRequest>>($"Found {nationalAdminResponse.Count()} BedRequests", true, nationalAdminResponse);
        }
    }
}



