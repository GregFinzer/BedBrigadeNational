using BedBrigade.Client.Services;
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

    public ContentDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService,
        ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _cachingService = cachingService;
        _contextFactory = contextFactory;
        _commonService = commonService;
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

            _cachingService.Set(cacheKey, result);
            return Task.FromResult(new ServiceResponse<Content>($"Found {GetEntityName()} with locationId of {locationId} and a name of {name}", true, result));
        }
    }

    public async Task<ServiceResponse<List<Content>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
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

    public async Task<ServiceResponse<Content>> GetByLocationAndContentType(int locationId, ContentType contentType)
    {
        return await GetAsync(contentType.ToString(), locationId);
    }
}


