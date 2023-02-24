
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using System.Security.Claims;
using System.Security.Principal;

namespace BedBrigade.Data.Services;

public class VolunteerDataService : IVolunteerDataService
{

    private readonly DataContext _context;
    private readonly AuthenticationStateProvider _auth;
    private ClaimsPrincipal _identity;

    public VolunteerDataService(DataContext context, AuthenticationStateProvider authProvider)
    {
        _context = context;
        _auth = authProvider;
        Task.Run(() => GetUserClaims(authProvider));
    }

    private async Task GetUserClaims(AuthenticationStateProvider provider)
    {
        var state = await provider.GetAuthenticationStateAsync();
        _identity = state.User;
    }

    public async Task<ServiceResponse<Volunteer>> GetAsync(int volunteerId)
    {
        var result = await _context.Volunteers.FindAsync(volunteerId);
        if (result != null)
        {
            return new ServiceResponse<Volunteer>("Found Record", true, result);
        }
        return new ServiceResponse<Volunteer>("Not Found");
    }

    public async Task<ServiceResponse<List<Volunteer>>> GetAllAsync()
    {
        var authState = await _auth.GetAuthenticationStateAsync();

        var role = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
        List<Volunteer> result;
        if (role.ToLower() != "national admin")
        {
            int.TryParse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0", out int locationId);
            result = _context.Volunteers.Where(v => v.LocationId == locationId).ToList();
        } 
        else
        {
            result = await _context.Volunteers.ToListAsync();
        }
        if (result != null)
        {
            return new ServiceResponse<List<Volunteer>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Volunteer>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int VolunteerId)
    {
        var volunteer = await _context.Volunteers.FindAsync(VolunteerId);
        if (volunteer == null)
        {
            return new ServiceResponse<bool>($"Volunteer record with key {VolunteerId} not found");
        }
        try
        {
            _context.Volunteers.Remove(volunteer);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {VolunteerId}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of user record with key {VolunteerId} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<Volunteer>> UpdateAsync(Volunteer volunteer)
    {
        int result = 0;
        var oldVolunteer = _context.Volunteers.Find(volunteer.VolunteerId);
        if(oldVolunteer != null)
        {
            oldVolunteer.LocationId = volunteer.LocationId;
            oldVolunteer.VolunteeringForDate = volunteer.VolunteeringForDate;
            oldVolunteer.IHaveVolunteeredBefore = volunteer.IHaveVolunteeredBefore;
            oldVolunteer.FirstName = volunteer.FirstName;
            oldVolunteer.VolunteeringForId = volunteer.VolunteeringForId;
            oldVolunteer.LastName = volunteer.LastName;
            oldVolunteer.Email = volunteer.Email;
            oldVolunteer.Phone = volunteer.Phone;
            oldVolunteer.OrganizationOrGroup = volunteer.OrganizationOrGroup;
            oldVolunteer.Message = volunteer.Message;
            oldVolunteer.IHaveAMinivan = volunteer.IHaveAMinivan;
            oldVolunteer.IHaveAnSUV = volunteer.IHaveAnSUV;
            oldVolunteer.IHaveAPickupTruck = volunteer.IHaveAPickupTruck;
        }
        try
        {

            await Task.Run(() => _context.Volunteers.Update(oldVolunteer));
            result = await _context.SaveChangesAsync();
        }
        catch(DbException ex)
        {
            Log.Logger.Error("Unable to save updated Volunteer record, {0}", ex);
        }
        catch(Exception ex)
        {
            Log.Logger.Error("Error while updating Volunteer record, {0}", ex.Message);
        }
        if (result == 1)
        {
            return new ServiceResponse<Volunteer>($"Updated volunteer with key {volunteer.VolunteerId}", true);
        }
        return new ServiceResponse<Volunteer>($"User with key {volunteer.VolunteerId} was not updated.");
    }

    public async Task<ServiceResponse<Volunteer>> CreateAsync(Volunteer volunteer)
    {
        try
        {
            await _context.Volunteers.AddAsync(volunteer);
            await _context.SaveChangesAsync();
            return new ServiceResponse<Volunteer>($"Added location with key {volunteer.VolunteerId}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Volunteer>($"DB error on delete of user record with key {volunteer.VolunteerId} - {ex.Message} ({ex.ErrorCode})");
        }

    }

}



