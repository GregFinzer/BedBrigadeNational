
using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class VolunteerDataService : IVolunteerDataService
{

    private readonly DataContext _context;

    public VolunteerDataService(DataContext context)
    {
        _context = context;
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
        var result = await _context.Volunteers.ToListAsync();
        if (result != null)
        {
            return new ServiceResponse<List<Volunteer>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Volunteer>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int VolunteerId)
    {
        var user = await _context.Users.FindAsync(VolunteerId);
        if (user == null)
        {
            return new ServiceResponse<bool>($"Volunteer record with key {VolunteerId} not found");
        }
        try
        {
            _context.Users.Remove(user);
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
        var result = await Task.Run(() => _context.Volunteers.Update(volunteer));
        if (result != null)
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



