using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;
using BedBrigade.Common;
using ContentType = BedBrigade.Common.Common.ContentType;

namespace BedBrigade.Data.Services;

public class ContentDataService : IContentDataService
{
    private const string FoundRecord = "Found Record";
    private const string CacheSection = "Content";
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly AuthenticationStateProvider _auth;

    private ClaimsPrincipal _identity { get; set; } = null;

    public ContentDataService(ICachingService cachingService, IDbContextFactory<DataContext> contextFactory, AuthenticationStateProvider authProvider)
    {
        _cachingService = cachingService;
        _contextFactory = contextFactory;
        _auth = authProvider;
        Task.Run(() => GetUserClaims(authProvider));
    }

    private async Task GetUserClaims(AuthenticationStateProvider provider)
    {
        var state = await provider.GetAuthenticationStateAsync();
        _identity = state.User;
    }

    public async Task<ServiceResponse<Content>> CreateAsync(Content content)
    {
        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                await ctx.Content.AddAsync(content);
                await ctx.SaveChangesAsync();
                _cachingService.ClearAll();
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
                _cachingService.ClearAll();
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
            var authState = await _auth.GetAuthenticationStateAsync();

            var role = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
            List<Content> result;
            if (role.ToLower() != RoleNames.NationalAdmin.ToLower())
            {
                int.TryParse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0", out int locationId);
                result = ctx.Content.Where(u => u.LocationId == locationId).ToList();
            }
            else
            {
                result = ctx.Content.ToList();
            }


            if (result != null)
            {
                return new ServiceResponse<List<Content>>($"Found {result.Count} records.", true, result);
            }

            return new ServiceResponse<List<Content>>("None found.");
        }
    }

    public async Task<ServiceResponse<Content>> GetAsync(int contentId)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, contentId);
        var cachedContent = _cachingService.Get<Content>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<Content>(FoundRecord, true, cachedContent); ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var result = await ctx.Content.FindAsync(contentId);
            if (result != null)
            {
                _cachingService.Set<Content>(cacheKey, result);
                return new ServiceResponse<Content>(FoundRecord, true, result);
            }

            return new ServiceResponse<Content>("Not Found");
        }
    }

    public async Task<ServiceResponse<Content>> GetAsync(string name, int location)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, location, name);
        var cachedContent = _cachingService.Get<Content>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<Content>(FoundRecord, true, cachedContent); 

        using (var ctx = _contextFactory.CreateDbContext())
        {
            //int locationId;
            Content result;
            //var authState = await _auth.GetAuthenticationStateAsync();
            //if (authState.User.Claims.ToList().Count == 0 )
            //{
            //    locationId = (await ctx.Locations.FirstOrDefaultAsync(l => l.Name.ToLower() == "national")).LocationId;
            //}
            //else
            //{
            //    var role = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
            //    locationId = int.Parse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0");
            //}
            result = await ctx.Content.FirstOrDefaultAsync(u => u.Name.ToLower() == name.ToLower() && u.LocationId == location);

            if (result != null)
            {
                _cachingService.Set<Content>(cacheKey, result);
                return new ServiceResponse<Content>(FoundRecord, true, result);
            }
            //if(name.ToLower() == "header" || name.ToLower() == "newpage")
            //{
            //    locationId = (await ctx.Locations.FirstOrDefaultAsync(l => l.Name.ToLower() == "national")).LocationId;
            //    result = ctx.Content.FirstOrDefault(u => u.LocationId == locationId && u.Name == name);
            //    return new ServiceResponse<Content>(FoundRecord, true, result);
            //}
            return new ServiceResponse<Content>("Not Found");
        }
    }

    public async Task<ServiceResponse<Content>> UpdateAsync(Content content)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var entity = await context.Content.FindAsync(content.ContentId);

            if (entity != null)
            {
                entity.ContentHtml = content.ContentHtml;
                entity.UpdateDate = DateTime.Now;
                entity.UpdateUser = content.UpdateUser;
                context.Entry(entity).State = EntityState.Modified;
               // context.Content.Update(entity);
                await context.SaveChangesAsync();
                _cachingService.ClearAll();
                return new ServiceResponse<Content>($"Content record was updated.", true, content);
            }
            return new ServiceResponse<Content>($"Content with key {content.ContentId} was not updated.");
        }
    }

    public async Task<ServiceResponse<Content>> GetAsync(string name)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, name);
        var cachedContent = _cachingService.Get<Content>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<Content>(FoundRecord, true, cachedContent);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            //int locationId;
            Content result;
            //var authState = await _auth.GetAuthenticationStateAsync();
            //if (authState.User.Claims.ToList().Count == 0 )
            //{
            //    locationId = (await ctx.Locations.FirstOrDefaultAsync(l => l.Name.ToLower() == "national")).LocationId;
            //}
            //else
            //{
            //    var role = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
            //    locationId = int.Parse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0");
            //}
            result = await ctx.Content.FirstOrDefaultAsync(u => u.Name == name);

            if (result != null)
            {
                _cachingService.Set<Content>(cacheKey, result);
                return new ServiceResponse<Content>(FoundRecord, true, result);
            }
            //if(name.ToLower() == "header" || name.ToLower() == "newpage")
            //{
            //    locationId = (await ctx.Locations.FirstOrDefaultAsync(l => l.Name.ToLower() == "national")).LocationId;
            //    result = ctx.Content.FirstOrDefault(u => u.LocationId == locationId && u.Name == name);
            //    return new ServiceResponse<Content>(FoundRecord, true, result);
            //}
            return new ServiceResponse<Content>("Not Found");
        }
    }

    public async Task<ServiceResponse<List<Content>>> GetAllAsync(ContentType type, int locationId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            //var authState = await _auth.GetAuthenticationStateAsync();

            //var role = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;
            //List<Content> result;
            //if (role.ToLower() != RoleNames.NationalAdmin.ToLower())
            //{
            //    int.TryParse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0", out int locationId);
            //    result = ctx.Content.Where(u => u.LocationId == locationId).ToList();
            //}
            //else
            //{
                var result = ctx.Content.Where(c => c.ContentType == type && c.LocationId == locationId).ToList();
            //}


            if (result != null)
            {
                return new ServiceResponse<List<Content>>($"Found {result.Count} records.", true, result);
            }

            return new ServiceResponse<List<Content>>("None found.");
        }

    }
}


