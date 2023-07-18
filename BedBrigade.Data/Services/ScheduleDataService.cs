using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class ScheduleDataService : IScheduleDataService
{
    private readonly DataContext _context;

    public ScheduleDataService(DataContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<Schedule>> GetAsync(int ScheduleId) // get single Schedule recird by ID
    {
        var result = await _context.Schedules.FindAsync(ScheduleId);
        if (result != null)
        {
            return new ServiceResponse<Schedule>("Found Schedule Record", true, result);
        }
        return new ServiceResponse<Schedule>("Schedule Record Not Found");
    } // Get record

    public async Task<ServiceResponse<List<Schedule>>> GetAllAsync() // Get all Schedule records (table "Schedule")
    {
        var result = await _context.Schedules.ToListAsync();
        if (result != null)
        {
            return new ServiceResponse<List<Schedule>>($"Found {result.Count} Schedule records.", true, result);
        }
        return new ServiceResponse<List<Schedule>>("None Schedule records found.");
    } // Get All Records

    public async Task<ServiceResponse<bool>> DeleteAsync(int ScheduleId) // Delete Schedule by ID
    {
        var mySchedule = await _context.Schedules.FindAsync(ScheduleId);
        if (mySchedule == null)
        {
            return new ServiceResponse<bool>($"Schedule record with ScheduleId = {ScheduleId} not found");
        }
        try
        {
            _context.Schedules.Remove(mySchedule);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed Schedule record with ID = {ScheduleId}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"Database error on delete of Schedule record with ID = {ScheduleId} - {ex.Message} ({ex.ErrorCode})");
        }
    } // Delete Schedule

    public async Task<ServiceResponse<Schedule>> UpdateAsync(Schedule Schedule) // update Schedule record (object)
    {
        try
        {
            var result = await Task.Run(() => _context.Schedules.Update(Schedule));
            await _context.SaveChangesAsync();
            if (result != null)
            {
                return new ServiceResponse<Schedule>($"Updated Schedule with ID = {Schedule.ScheduleId}", true);
            }
            return new ServiceResponse<Schedule>($"Schedule with ID = {Schedule.ScheduleId} was not updated.");
        }
        catch(Exception ex)
        {
            return new ServiceResponse<Schedule>($"Schedule with ID = {Schedule.ScheduleId} was not updated: "+ex.Message);
        }
    } // update Schedule record

    public async Task<ServiceResponse<Schedule>> CreateAsync(Schedule Schedule) // add new Schedule object
    {
        try
        {
            await _context.Schedules.AddAsync(Schedule);
            await _context.SaveChangesAsync();
            return new ServiceResponse<Schedule>($"Added new Schedule with ID =  {Schedule.ScheduleId}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Schedule>($"Database error: new  Schedule cannot be added - {ex.Message} ({ex.ErrorCode})");
        }
    } // add new Schedule

} // end class ScheduleDataService



