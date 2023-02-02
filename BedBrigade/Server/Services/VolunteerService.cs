using BedBrigade.Server.Controllers;
using BedBrigade.Server.Data;
using BedBrigade.Shared;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Server.Services;

public class VolunteerService : IVolunteerService
{

    private readonly DataContext _context;
    private readonly ILogger<VolunteerController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VolunteerService(DataContext context
        , ILogger<VolunteerController> logger
        , IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
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
        var result = _context.Volunteers.ToList();
        if (result != null)
        {
            return new ServiceResponse<List<Volunteer>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Volunteer>>("None found.");
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

    public async Task<ServiceResponse<Volunteer>> UpdateAsync(Volunteer volunteer)
    {
        var result = _context.Volunteers.Update(volunteer);
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



