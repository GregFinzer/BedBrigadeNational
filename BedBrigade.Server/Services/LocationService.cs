using BedBrigade.Server.Controllers;
using BedBrigade.Server.Data;
using BedBrigade.Shared;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Server.Services;

public class LocationService : ILocationService
{
    private readonly DataContext _context;
    private readonly ILogger<LocationController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocationService(DataContext context
        , ILogger<LocationController> logger
        , IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResponse<Location>> GetAsync(int locationId)
    {
        var result = await _context.Locations.FindAsync(locationId);
        if (result != null)
        {
            return new ServiceResponse<Location>("Found Record", true, result);
        }
        return new ServiceResponse<Location>("Not Found");
    }

    public async Task<ServiceResponse<Location>> GetAsync(string locationName)
    {
        var result = await _context.Locations.FirstOrDefaultAsync(l => l.Name == locationName);
        if (result != null)
        {
            return new ServiceResponse<Location>("Found Record", true, result);
        }
        return new ServiceResponse<Location>("Not Found");
    }

    public async Task<ServiceResponse<List<Location>>> GetAllAsync()
    {
        var result = _context.Locations.ToList();
        if (result != null)
        {
            return new ServiceResponse<List<Location>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Location>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(string UserName)
    {
        var user = await _context.Users.FindAsync(UserName);
        if (user == null)
        {
            return new ServiceResponse<bool>($"User record with key {UserName} not found");
        }
        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {UserName}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of user record with key {UserName} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<Location>> UpdateAsync(Location location)
    {
        var result = _context.Locations.Update(location);
        if (result != null)
        {
            return new ServiceResponse<Location>($"Updated location with key {location.Name}", true);
        }
        return new ServiceResponse<Location>($"User with key {location.Name} was not updated.");
    }

    public async Task<ServiceResponse<Location>> CreateAsync(Location location)
    {
        try
        {
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();
            return new ServiceResponse<Location>($"Added location with key {location.Name}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Location>($"DB error on delete of user record with key {location.Name} - {ex.Message} ({ex.ErrorCode})");
        }

    }


}



