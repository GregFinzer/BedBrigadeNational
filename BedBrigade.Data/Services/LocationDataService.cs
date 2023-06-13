using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using System.Runtime.InteropServices;
using BedBrigade.Common;
using KellermanSoftware.AddressParser;

namespace BedBrigade.Data.Services;

public class LocationDataService : BaseDataService, ILocationDataService
{
    private const string CacheSection = "Location";
    private const string FoundRecord = "Found Record";
    private readonly ICachingService _cachingService;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IDbContextFactory<DataContext> _contextFactory;

    public LocationDataService(ICachingService cachingService, 
        IConfigurationDataService configurationDataService,
        IDbContextFactory<DataContext> contextFactory, 
        AuthenticationStateProvider authProvider) : base(authProvider)
    {
        _cachingService = cachingService;
        _configurationDataService = configurationDataService;
        _contextFactory = contextFactory;
    }

    public async Task<ServiceResponse<Location>> CreateAsync(Location location)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            try
            {
                await ctx.Locations.AddAsync(location);
                await ctx.SaveChangesAsync();
                _cachingService.ClearAll();
                return new ServiceResponse<Location>($"Added location record with key {location.Name}.", true, location);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<Location>($"DB error on create of location record {location.Name} - {ex.Message} ({ex.ErrorCode})");
            }
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int locationId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {

            var location = await ctx.Locations.FindAsync(locationId);
            if (location == null)
            {
                return new ServiceResponse<bool>($"User record with key {locationId} not found");
            }
            try
            {
                ctx.Locations.Remove(location);
                await ctx.SaveChangesAsync();
                _cachingService.ClearAll();
                return new ServiceResponse<bool>($"Removed record with key {locationId}.", true);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<bool>($"DB error on delete of user record with key {locationId} - {ex.Message} ({ex.ErrorCode})");
            }
        }
    }

    public async Task<ServiceResponse<List<Location>>> GetAllAsync()
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, "GetAllAsync");
        var cachedLocations = _cachingService.Get<List<Location>>(cacheKey);

        if (cachedLocations != null)
            return new ServiceResponse<List<Location>>($"Found {cachedLocations.Count} records.", true, cachedLocations); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var result = await ctx.Locations.ToListAsync();
            if (result != null)
            {
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<Location>>($"Found {result.Count} records.", true, result);
            }
            return new ServiceResponse<List<Location>>("None found.");
        }
    }

    public async Task<ServiceResponse<Location>> GetAsync(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, locationId);
        var cachedLocation = _cachingService.Get<Location>(cacheKey);

        if (cachedLocation != null)
            return new ServiceResponse<Location>(FoundRecord, true, cachedLocation); 

        using (var ctx = _contextFactory.CreateDbContext())
        {

            var result = await ctx.Locations.FindAsync(locationId);
            if (result != null)
            {
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<Location>("Found Record", true, result);
            }
            return new ServiceResponse<Location>("Not Found");
        }
    }

    public async Task<ServiceResponse<Location>> GetLocationByRouteAsync(string routeName)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, routeName);
        var cachedLocation = _cachingService.Get<Location>(cacheKey);

        if (cachedLocation != null)
            return new ServiceResponse<Location>($"{routeName} found", true, cachedLocation);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var loc = await ctx.Locations.FirstOrDefaultAsync(l => l.Route == routeName);
            if (loc != null)
            {
                _cachingService.Set(cacheKey, loc);
                return new ServiceResponse<Location>($"{routeName} found", true, loc);
            }
            return new ServiceResponse<Location>("Not Found");
        }
    }


    public async Task<ServiceResponse<Location>> UpdateAsync(Location location)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {

            var loc = await ctx.Locations.FindAsync(location.LocationId);
            if (loc != null)
            {
                var result = await UpdateLocationAsync(loc, location);
                if (result.Success)
                {
                    _cachingService.ClearAll();
                    return result;
                }
            }
        }
        return new ServiceResponse<Location>($"User with key {location.LocationId} was not updated.");

    }

    public async Task<ServiceResponse<List<LocationDistance>>> GetBedBrigadeNearMe(string postalCode)
    {
        AddressParser parser = LibraryFactory.CreateAddressParser();

        if (!parser.IsValidZipCode(postalCode))
        {
            return new ServiceResponse<List<LocationDistance>>($"Invalid postal code {postalCode}");
        }

        var zipCodeInfo = parser.GetInfoForZipCode(postalCode);
        
        if (zipCodeInfo.Latitude == null || zipCodeInfo.Longitude == null)
        {
            return new ServiceResponse<List<LocationDistance>>($"Postal code {postalCode} is a military or PO Box and cannot be used");
        }

        int maxMiles;
        var milesResult = await _configurationDataService.GetAsync(ConfigNames.BedBrigadeNearMeMaxMiles);
        if (milesResult.Success && milesResult.Data != null && milesResult.Data.ConfigurationValue != null)
        {
            maxMiles = int.Parse(milesResult.Data.ConfigurationValue);
        }
        else
        {
            return new ServiceResponse<List<LocationDistance>>($"BedBrigadeNearMeMaxMiles is not set or is not an integer");
        }

        var locationsResult = await GetAllAsync();
        bool anyPostalCodes = locationsResult.Data.Any(l => !string.IsNullOrEmpty(l.PostalCode));

        if (!anyPostalCodes)
        {
            return new ServiceResponse<List<LocationDistance>>($"No locations have postal codes");
        }

        var result = new List<LocationDistance>();
        foreach (var loc in locationsResult.Data)
        {
            if (!string.IsNullOrEmpty(loc.PostalCode))
            {
                var distance = parser.GetDistanceInMilesBetweenTwoZipCodes(postalCode, loc.PostalCode);
                if (distance < maxMiles)
                {
                    LocationDistance locationDistance = new LocationDistance();
                    locationDistance.LocationId = loc.LocationId;
                    locationDistance.Name = loc.Name;
                    locationDistance.Route = loc.Route;
                    locationDistance.Distance = distance;
                    result.Add(locationDistance);
                }
            }
        }
        result = result.OrderBy(r => r.Distance).ToList();

        if (result.Count == 0)
        {
            return new ServiceResponse<List<LocationDistance>>($"No locations found within {maxMiles} miles of {postalCode}", true, result);
        }

        return new ServiceResponse<List<LocationDistance>>($"Found {result.Count} locations within {maxMiles} miles of {postalCode}", true, result);
    }

    private async Task<ServiceResponse<Location>> UpdateLocationAsync(Location loc, Location location)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            try
            {
                ctx.Entry(loc).CurrentValues.SetValues(location);

                ctx.Entry(loc).State = EntityState.Modified;
                ctx.Locations.Update(loc);
                var result = await ctx.SaveChangesAsync();
                if (result > 0)
                {
                    _cachingService.ClearAll();
                    return new ServiceResponse<Location>($"Updated location with key {location.LocationId}", true, location);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error updating location {0}", ex.ToString);
            }
            return new ServiceResponse<Location>($"User with key {location.LocationId} was not updated.");

        }

    }


}





