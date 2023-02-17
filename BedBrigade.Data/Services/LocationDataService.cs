using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class LocationDataService : BaseDataService, ILocationDataService
{
    private readonly DataContext _context;

    public LocationDataService(DataContext context, AuthenticationStateProvider authProvider) : base(authProvider)
    {
        _context= context;
    }

    public async Task<ServiceResponse<Location>> CreateAsync(Location location)
    {
        try
        {
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();
            return new ServiceResponse<Location>($"Added location record with key {location.Name}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Location>($"DB error on create of location record {location.Name} - {ex.Message} ({ex.ErrorCode})");
        }
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
        var result = await _context.Locations.ToListAsync();
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
        var loc = await _context.Locations.FindAsync(location.LocationId);
        if (loc != null)
        {
            loc.Name = location.Name;
            loc.Route = location.Route;
            loc.Address1 = location.Address1;
            loc.Address2 = location.Address2;
            loc.City = location.City;
            loc.State = location.State;
            loc.PostalCode = location.PostalCode;
            loc.Latitude = location.Latitude;
            loc.Longitude = location.Longitude;
        }
        try
        {

            _context.Entry(loc).State = EntityState.Modified;
            var result = await Task.Run(() => _context.Locations.Update(location));
            await _context.SaveChangesAsync();
            if (result != null)
            {
                return new ServiceResponse<Location>($"Updated location with key {location.LocationId}", true);
            }
        }
        catch (DbException ex)
        {
            Log.Logger.Error("Database error updating location {0}", ex.ToString);
        }
        catch(Exception ex)
        {
            Log.Logger.Error("Error updating location {0}", ex.ToString);

        }
        return new ServiceResponse<Location>($"User with key {location.LocationId} was not updated.");
    }
}



