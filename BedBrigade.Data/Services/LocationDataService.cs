using BedBrigade.Data.Models;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class LocationDataService : ILocationDataService
{
    private readonly DataContext _context;

    public LocationDataService(DataContext context)
    {
        _context= context;
    }

    public Task<ServiceResponse<Location>> CreateAsync(Location location)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int locationId)
    {
        var location = await _context.Locations.FindAsync(locationId);
        if (location == null)
        {
            return new ServiceResponse<bool>($"User record with key {locationId} not found");
        }
        try
        {
            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {locationId}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of user record with key {locationId} - {ex.Message} ({ex.ErrorCode})");
        }
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

    public async Task<ServiceResponse<Location>> GetAsync(int locationId)
    {
        var result = await _context.Locations.FindAsync(locationId);
        if (result != null)
        {
            return new ServiceResponse<Location>("Found Record", true, result);
        }
        return new ServiceResponse<Location>("Not Found");
    }


    public async Task<ServiceResponse<Location>> UpdateAsync(Location location)
    {
        var result = _context.Locations.Update(location);
        if (result != null)
        {
            return new ServiceResponse<Location>($"Updated location with key {location.LocationId}", true);
        }
        return new ServiceResponse<Location>($"User with key {location.LocationId} was not updated.");
    }
}



