
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BedBrigade.Common;

namespace BedBrigade.Data.Services;

public class DonationDataService : Repository<Donation>, IDonationDataService
{

    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly AuthenticationStateProvider _auth;

    public DonationDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _auth = authProvider;
    }

    public async Task<ServiceResponse<List<Donation>>> GetAllForLocationAsync()
    {
        AuthenticationState authState = await _auth.GetAuthenticationStateAsync();

        Claim? roleClaim = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        if (roleClaim == null)
            return new ServiceResponse<List<Donation>>("No Claim of type Role found");
        string roleName = roleClaim.Value;

        Claim? locationClaim = authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId");

        if (locationClaim == null)
            return new ServiceResponse<List<Donation>>("No Claim of type LocationId found");

        int.TryParse(locationClaim.Value ?? "0", out int locationId);

        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetAllForLocationAsync with LocationId ({locationId})");
        var cachedContent = _cachingService.Get<List<Donation>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<Donation>>($"Found {cachedContent.Count} {GetEntityName()} records in cache", true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Donation>();
            if (roleName.ToLower() != RoleNames.NationalAdmin.ToLower())
            {
                var result = await dbSet.Where(b => b.LocationId == locationId).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Donation>>($"Found {result.Count()} {GetEntityName()} records", true, result);
            }

            var nationalAdminResponse = await dbSet.ToListAsync();
            _cachingService.Set(cacheKey, nationalAdminResponse);
            return new ServiceResponse<List<Donation>>($"Found {nationalAdminResponse.Count()} {GetEntityName()} records", true, nationalAdminResponse);
        }
    }




}



