using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using System.Runtime.InteropServices;
using BedBrigade.Common;
using KellermanSoftware.AddressParser;

namespace BedBrigade.Data.Services;

public class LocationDataService : Repository<Location>, ILocationDataService
{
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly AuthenticationStateProvider _auth;
    private readonly IConfigurationDataService _configurationDataService;


    public LocationDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService, 
        AuthenticationStateProvider authProvider,
        IConfigurationDataService configurationDataService) : base(contextFactory, cachingService, authProvider)
    {
        _cachingService = cachingService;
        _configurationDataService = configurationDataService;
        _contextFactory = contextFactory;
    }









    public async Task<ServiceResponse<Location>> GetLocationByRouteAsync(string routeName)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(),  $"GetLocationByRouteAsync for ({routeName})");
        var cachedLocation = _cachingService.Get<Location>(cacheKey);

        if (cachedLocation != null)
            return new ServiceResponse<Location>($"{routeName} found in cache", true, cachedLocation);

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
        var milesResult = await _configurationDataService.GetByIdAsync(ConfigNames.BedBrigadeNearMeMaxMiles);
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




}





