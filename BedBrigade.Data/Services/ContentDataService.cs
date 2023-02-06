using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class ContentDataService : IContentDataService
{
    private readonly DataContext _context;

    public ContentDataService(DataContext context)
    {
        _context = context;
    }

    public Task<ServiceResponse<Content>> CreateAsync(Content content)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int contentId)
    {
        var content = await _context.Content.FindAsync(contentId);
        if (content == null)
        {
            return new ServiceResponse<bool>($"User record with key {contentId} not found");
        }
        try
        {
            _context.Content.Remove(content);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {contentId}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of user record with key {contentId} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<List<Content>>> GetAllAsync()
    {
        var result = _context.Content.ToList();
        if (result != null)
        {
            return new ServiceResponse<List<Content>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Content>>("None found.");

    }

    public async Task<ServiceResponse<Content>> GetAsync(int contentId)
    {
        var result = await _context.Content.FindAsync(contentId);
        if (result != null)
        {
            return new ServiceResponse<Content>("Found Record", true, result);
        }
        return new ServiceResponse<Content>("Not Found");
    }

    public async Task<ServiceResponse<Content>> GetAsync(string name)
    {
        var result = await _context.Content.FirstOrDefaultAsync(c => c.Name == name);
        if (result != null)
        {
            return new ServiceResponse<Content>("Found Record", true, result);
        }
        return new ServiceResponse<Content>("Not Found");
    }

    public async Task<ServiceResponse<Content>> UpdateAsync(Content content)
    {
        var result = _context.Content.Update(content);
        if (result != null)
        {
            return new ServiceResponse<Content>($"Updated content with key {content.ContentId}", true);
        }
        return new ServiceResponse<Content>($"User with key {content.ContentId} was not updated.");
    }
}



