using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class ContentDataService : Repository<Content>, IContentDataService
{
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICommonService _commonService;
    private readonly ITimezoneDataService _timezoneDataService;
    
    

    public ContentDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService,
        ICommonService commonService,
        ITimezoneDataService timezoneDataService) : base(contextFactory, cachingService, authService)
    {
        _cachingService = cachingService;
        _contextFactory = contextFactory;
        _commonService = commonService;
        _timezoneDataService = timezoneDataService;
    }

    public Task<ServiceResponse<Content>> GetAsync(string name, int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), locationId, name);
        var cachedContent = _cachingService.Get<Content>(cacheKey);

        if (cachedContent != null)
        {
            return Task.FromResult(new ServiceResponse<Content>($"Found {GetEntityName()} in cache", true, cachedContent));
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Content>();
            var result = dbSet.Where(c => c.Name == name && c.LocationId == locationId).FirstOrDefault();

            if (result == null)
            {
                return Task.FromResult(new ServiceResponse<Content>($"Could not find {GetEntityName()} with locationId of {locationId} and a name of {name}", false, null));
            }

            _timezoneDataService.FillLocalDates(result);

            _cachingService.Set(cacheKey, result);
            return Task.FromResult(new ServiceResponse<Content>($"Found {GetEntityName()} with locationId of {locationId} and a name of {name}", true, result));
        }
    }

    public async Task<ServiceResponse<List<Content>>> GetAllExceptBlogTypes()
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), "GetAllExceptBlogTypes");
        var cachedContent = _cachingService.Get<List<Content>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Content>>($"Found {cachedContent.Count()} {GetEntityName()} in cache", true, cachedContent);
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Content>();
            var result = await dbSet.Where(c => !BlogTypes.ValidBlogTypes.Contains(c.ContentType)).ToListAsync();

            if (result == null)
            {
                return new ServiceResponse<List<Content>>($"Could not find {GetEntityName()} for GetAllExceptBlogTypes", false, null);
            }

            _timezoneDataService.FillLocalDates(result);

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<Content>>($"Found {result.Count()} {GetEntityName()}", true, result);
        }
    }

    public async Task<ServiceResponse<List<Content>>> GetForLocationExceptBlogTypes(int locationId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetForLocationExceptBlogTypes({locationId})");
        var cachedContent = _cachingService.Get<List<Content>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Content>>($"Found {cachedContent.Count()} {GetEntityName()} in cache", true, cachedContent);
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Content>();
            var result = await dbSet.Where(c => c.LocationId == locationId && !BlogTypes.ValidBlogTypes.Contains(c.ContentType)).ToListAsync();

            if (result == null)
            {
                return new ServiceResponse<List<Content>>($"Could not find {GetEntityName()} for GetForLocationExceptBlogTypes", false, null);
            }

            _timezoneDataService.FillLocalDates(result);

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<Content>>($"Found {result.Count()} {GetEntityName()}", true, result);
        }
    }

    public async Task<ServiceResponse<List<Content>>> GetByContentType(ContentType contentType)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetByContentType({contentType})");
        var cachedContent = _cachingService.Get<List<Content>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Content>>($"Found {cachedContent.Count()} {GetEntityName()} in cache", true, cachedContent);
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Content>();
            var result = await dbSet.Where(c => c.ContentType == contentType).ToListAsync();

            if (result == null)
            {
                return new ServiceResponse<List<Content>>($"Could not find {GetEntityName()} for GetByContentType", false, null);
            }

            _timezoneDataService.FillLocalDates(result);

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<Content>>($"Found {result.Count()} {GetEntityName()}", true, result);
        }
    }

    public async Task<ServiceResponse<List<Content>>> GetByLocationContentType(int locationId, ContentType contentType)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetByLocationContentType({locationId}, {contentType})");
        var cachedContent = _cachingService.Get<List<Content>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<Content>>($"Found {cachedContent.Count()} {GetEntityName()} in cache", true, cachedContent);
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Content>();
            var result = await dbSet.Where(c => c.LocationId == locationId && c.ContentType == contentType).ToListAsync();

            if (result == null)
            {
                return new ServiceResponse<List<Content>>($"Could not find {GetEntityName()} for GetByLocationContentType", false, null);
            }

            _timezoneDataService.FillLocalDates(result);

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<Content>>($"Found {result.Count()} {GetEntityName()}", true, result);
        }
    }

    public override async Task<ServiceResponse<List<Content>>> GetAllAsync()
    {
        var result = await base.GetAllAsync();

        if (result.Success && result.Data != null)
        {
            _timezoneDataService.FillLocalDates(result.Data);
        }

        return result;
    }


    public override Task<ServiceResponse<Content>> CreateAsync(Content entity)
    {
        entity.ContentHtml = RemoveSyncFusionClasses(entity.ContentHtml);
        return base.CreateAsync(entity);
    }

    public override async Task<ServiceResponse<Content>> UpdateAsync(Content content)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var entity = await context.Content.FindAsync(content.ContentId);

            if (entity != null)
            {
                entity.Name = content.Name;
                entity.Title = content.Title;
                entity.ContentHtml = StringUtil.RestoreHrefWithJavaScript(entity.ContentHtml, content.ContentHtml);
                entity.ContentHtml = RemoveSyncFusionClasses(entity.ContentHtml);
                entity.UpdateDate = DateTime.UtcNow;
                entity.UpdateUser = content.UpdateUser;
                entity.MainImageFileName = content.MainImageFileName;
                context.Entry(entity).State = EntityState.Modified;
                await context.SaveChangesAsync();
                _cachingService.ClearByEntityName(GetEntityName());
                return new ServiceResponse<Content>($"Content record was updated.", true, content);
            }
            return new ServiceResponse<Content>($"Content with key {content.ContentId} was not updated.");
        }
    }

    private string RemoveSyncFusionClasses(string? input)
    {
        if (input == null)
        {
            return string.Empty;
        }

        return input.Replace("e-rte-image", "").Replace("e-imginline", "");
    }

    public async Task<ServiceResponse<Content>> GetSingleByLocationAndContentType(int locationId, ContentType contentType)
    {
        return await GetAsync(contentType.ToString(), locationId);
    }

    public async Task<ServiceResponse<List<BlogItem>>> GetTopBlogItems(int locationId, ContentType contentType)
    {
        var result = await GetBlogItems(locationId, contentType);
        if (result.Success && result.Data != null)
        {
            var topBlogItems = result.Data.OrderByDescending(o => o.UpdateDate).Take(Defaults.MaxTopBlogItems).ToList();
            return new ServiceResponse<List<BlogItem>>($"Found {topBlogItems.Count()} {GetEntityName()}", true, topBlogItems);
        }

        return result;
    }

    public async Task<ServiceResponse<List<BlogItem>>> GetOlderBlogItems(int locationId, ContentType contentType)
    {
        var result = await GetBlogItems(locationId, contentType);
        if (result.Success && result.Data != null)
        {
            var topBlogItems = result.Data.OrderByDescending(o => o.UpdateDate).Skip(Defaults.MaxTopBlogItems).ToList();
            return new ServiceResponse<List<BlogItem>>($"Found {topBlogItems.Count()} {GetEntityName()}", true, topBlogItems);
        }

        return result;
    }

    public async Task<ServiceResponse<List<BlogItem>>> GetBlogItems(int locationId, ContentType contentType)
    {
        const int truncationLength = 188;

        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetBlogItems({locationId}, {contentType}");
        var cachedContent = _cachingService.Get<List<BlogItem>>(cacheKey);

        if (cachedContent != null)
        {
            return new ServiceResponse<List<BlogItem>>($"Found {GetEntityName()} in cache", true, cachedContent);
        }

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var dbSet = ctx.Set<Content>();
            var result = await dbSet.Where(c => c.ContentType == contentType && c.LocationId == locationId)
                .OrderByDescending(o => o.UpdateDate)
                .ToListAsync();

            if (result == null)
            {
                return new ServiceResponse<List<BlogItem>>($"Could not find {GetEntityName()} with locationId of {locationId} and a contentType of {contentType}", false, null);
            }

            _timezoneDataService.FillLocalDates(result);

            List<BlogItem> blogItems = new List<BlogItem>();

            foreach (var item in result)
            {
                blogItems.Add(new BlogItem
                {
                    ContentId = item.ContentId,
                    LocationId = item.LocationId,
                    ContentType = item.ContentType,
                    ContentHtml = item.ContentHtml,
                    Name = item.Name,
                    Title = item.Title,
                    MainImageFileName = item.MainImageFileName,
                    CreateDate = item.CreateDate,
                    CreateDateLocal = item.CreateDateLocal,
                    CreateUser = item.CreateUser,
                    UpdateDate = item.UpdateDate,
                    MachineName = item.MachineName,
                    UpdateDateLocal = item.UpdateDateLocal,
                    UpdateUser = item.UpdateUser,
                    Description = StringUtil.TruncateTextToLastWord(WebHelper.StripHTML(item.ContentHtml), truncationLength),
                });
            }

            _cachingService.Set(cacheKey, blogItems);
            return new ServiceResponse<List<BlogItem>>($"Found {GetEntityName()} with locationId of {locationId} and a contentType of {contentType}", true, blogItems);
        }

    }
}


