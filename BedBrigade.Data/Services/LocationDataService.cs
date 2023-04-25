using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Serilog;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class LocationDataService : BaseDataService, ILocationDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;

    public LocationDataService(IDbContextFactory<DataContext> contextFactory, AuthenticationStateProvider authProvider) : base(authProvider)
    {
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
                return new ServiceResponse<Location>($"Added location record with key {location.Name}.", true);
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
        using (var ctx = _contextFactory.CreateDbContext())
        {

            var result = await ctx.Locations.ToListAsync();
            if (result != null)
            {
                return new ServiceResponse<List<Location>>($"Found {result.Count} records.", true, result);
            }
            return new ServiceResponse<List<Location>>("None found.");
        }
    }

    public async Task<ServiceResponse<Location>> GetAsync(int locationId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {

            var result = await ctx.Locations.FindAsync(locationId);
            if (result != null)
            {
                return new ServiceResponse<Location>("Found Record", true, result);
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
                ctx.Entry(loc).CurrentValues.SetValues(location);
                try
                {

                    ctx.Entry(loc).State = EntityState.Modified;
                    ctx.Locations.Update(loc);
                    var result = await ctx.SaveChangesAsync();
                    if (result > 0)
                    {
                        return new ServiceResponse<Location>($"Updated location with key {location.LocationId}", true);
                    }
                }
                catch (DbException ex)
                {
                    Log.Logger.Error("Database error updating location {0}", ex.ToString);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Error updating location {0}", ex.ToString);

                }
            }
            return new ServiceResponse<Location>($"User with key {location.LocationId} was not updated.");
        }
    }
}



