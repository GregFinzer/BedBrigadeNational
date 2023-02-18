
using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class VolunteerForDataService : IVolunteerForDataService
{
    private readonly DataContext _context;

    public VolunteerForDataService(DataContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<VolunteerFor>> GetAsync(int volunteerForId)
    {
        var result = await _context.VolunteersFor.FindAsync(volunteerForId);
        if (result != null)
        {
            return new ServiceResponse<VolunteerFor>("Found Record", true, result);
        }
        return new ServiceResponse<VolunteerFor>("Not Found");
    }

    public async Task<ServiceResponse<List<VolunteerFor>>> GetAllAsync()
    {
        var result = await _context.VolunteersFor.ToListAsync();
        if (result != null)
        {
            return new ServiceResponse<List<VolunteerFor>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<VolunteerFor>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int VolunteerForId)
    {
        var volunteer = await _context.VolunteersFor.FindAsync(VolunteerForId);
        if (volunteer == null)
        {
            return new ServiceResponse<bool>($"Volunteer record with key {VolunteerForId} not found");
        }
        try
        {
            _context.VolunteersFor.Remove(volunteer);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {VolunteerForId}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of user record with key {VolunteerForId} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<VolunteerFor>> UpdateAsync(VolunteerFor volunteerFor)
    {
        var result = await Task.Run(() => _context.VolunteersFor.Update(volunteerFor));
        if (result != null)
        {
            return new ServiceResponse<VolunteerFor>($"Updated volunteer with key {volunteerFor.VolunteerForId}", true);
        }
        return new ServiceResponse<VolunteerFor>($"User with key {volunteerFor.VolunteerForId} was not updated.");
    }

    public async Task<ServiceResponse<VolunteerFor>> CreateAsync(VolunteerFor volunteerFor)
    {
        try
        {
            await _context.VolunteersFor.AddAsync(volunteerFor);
            await _context.SaveChangesAsync();
            return new ServiceResponse<VolunteerFor>($"Added location with key {volunteerFor.VolunteerForId}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<VolunteerFor>($"DB error on delete of user record with key {volunteerFor.VolunteerForId} - {ex.Message} ({ex.ErrorCode})");
        }

    }

}



