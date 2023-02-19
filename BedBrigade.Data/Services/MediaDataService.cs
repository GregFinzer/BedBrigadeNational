
using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class MediaDataService : IMediaDataService
{
    private readonly DataContext _context;

    public MediaDataService(DataContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<Media>> GetAsync(int mediaId) // get single media recird by ID
    {
        var result = await _context.Media.FindAsync(mediaId);
        if (result != null)
        {
            return new ServiceResponse<Media>("Found Media Record", true, result);
        }
        return new ServiceResponse<Media>("Media Record Not Found");
    } // Get record

    public async Task<ServiceResponse<List<Media>>> GetAllAsync() // Get all Media records (table "Media")
    {
        var result = await _context.Media.ToListAsync();
        if (result != null)
        {
            return new ServiceResponse<List<Media>>($"Found {result.Count} media records.", true, result);
        }
        return new ServiceResponse<List<Media>>("None Media records found.");
    } // Get All Records

    public async Task<ServiceResponse<bool>> DeleteAsync(int mediaId) // Delete media by ID
    {
        var myMedia = await _context.Media.FindAsync(mediaId);
        if (myMedia == null)
        {
            return new ServiceResponse<bool>($"Media record with MediaId = {mediaId} not found");
        }
        try
        {
            _context.Media.Remove(myMedia);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed media record with ID = {mediaId}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"Database error on delete of media record with ID = {mediaId} - {ex.Message} ({ex.ErrorCode})");
        }
    } // Delete media

    public async Task<ServiceResponse<Media>> UpdateAsync(Media media) // update Media record (object)
    {
        var result = await Task.Run(() =>_context.Media.Update(media));
        if (result != null)
        {
            return new ServiceResponse<Media>($"Updated Media with ID = {media.MediaId}", true);
        }
        return new ServiceResponse<Media>($"Media with ID = {media.MediaId} was not updated.");
    } // update media record

    public async Task<ServiceResponse<Media>> CreateAsync(Media media) // add new media object
    {
        try
        {
            await _context.Media.AddAsync(media);
            await _context.SaveChangesAsync();
            return new ServiceResponse<Media>($"Added new Media with ID =  {media.MediaId}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Media>($"Database error: new  media cannot be added - {ex.Message} ({ex.ErrorCode})");
        }
    } // add new media

} // end class MediaDataService



