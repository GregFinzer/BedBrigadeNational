using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class ContentDataService : IContentDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;

    public ContentDataService(IDbContextFactory<DataContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ServiceResponse<Content>> CreateAsync(Content content)
    {
        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                await ctx.Content.AddAsync(content);
                await ctx.SaveChangesAsync();
                return new ServiceResponse<Content>($"Added content with key {content.Name}.", true);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Content>($"DB error on create of content record {content.Name} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int contentId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var content = await ctx.Content.FindAsync(contentId);
            if (content == null)
            {
                return new ServiceResponse<bool>($"User record with key {contentId} not found");
            }

            try
            {
                ctx.Content.Remove(content);
                await ctx.SaveChangesAsync();
                return new ServiceResponse<bool>($"Removed record with key {contentId}.", true);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<bool>(
                    $"DB error on delete of user record with key {contentId} - {ex.Message} ({ex.ErrorCode})");
            }
        }
    }

    public async Task<ServiceResponse<List<Content>>> GetAllAsync()
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var result = await ctx.Content.ToListAsync();
            if (result != null)
            {
                return new ServiceResponse<List<Content>>($"Found {result.Count} records.", true, result);
            }

            return new ServiceResponse<List<Content>>("None found.");
        }
    }

    public async Task<ServiceResponse<Content>> GetAsync(int contentId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var result = await ctx.Content.FindAsync(contentId);
            if (result != null)
            {
                return new ServiceResponse<Content>("Found Record", true, result);
            }

            return new ServiceResponse<Content>("Not Found");
        }
    }

    public async Task<ServiceResponse<Content>> GetAsync(string name)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var result = await ctx.Content.FirstOrDefaultAsync(c => c.Name == name);
            if (result != null)
            {
                return new ServiceResponse<Content>("Found Record", true, result);
            }

            return new ServiceResponse<Content>("Not Found");
        }
    }

    public async Task<ServiceResponse<Content>> UpdateAsync(Content content)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var result = await Task.Run(() => ctx.Content.Update(content));
            if (result != null)
            {
                return new ServiceResponse<Content>($"Updated content with key {content.ContentId}", true);
            }

            return new ServiceResponse<Content>($"User with key {content.ContentId} was not updated.");
        }
    }
}



