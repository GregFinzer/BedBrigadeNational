using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using BedBrigade.Common;
using ContentType = BedBrigade.Common.Common.ContentType;

namespace BedBrigade.Data.Services;

public class TemplateDataService : ITemplateDataService
{
    private const string FoundRecord = "Found Record";
    private const string CacheSection = "Template";
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;

    public TemplateDataService(ICachingService cachingService, IDbContextFactory<DataContext> contextFactory)
    {
        _cachingService = cachingService;
        _contextFactory = contextFactory;
    }

    public async Task<ServiceResponse<Template>> CreateAsync(Template template)
    {
        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                await ctx.Templates.AddAsync(template);
                await ctx.SaveChangesAsync();
                _cachingService.ClearAll();
                return new ServiceResponse<Template>($"Added template with key {template.Name}.", true);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Template>(
                $"DB error on create of template record {template.Name} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int templateId)
    {
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var template = await ctx.Templates.FindAsync(templateId);
            if (template == null)
            {
                return new ServiceResponse<bool>($"User record with key {templateId} not found");
            }

            try
            {
                ctx.Templates.Remove(template);
                await ctx.SaveChangesAsync();
                _cachingService.ClearAll();
                return new ServiceResponse<bool>($"Removed record with key {templateId}.", true);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<bool>(
                    $"DB error on delete of user record with key {templateId} - {ex.Message} ({ex.ErrorCode})");
            }
        }
    }

    public async Task<ServiceResponse<List<Template>>> GetAllAsync()
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, "GetAllAsync");
        var cachedTemplates = _cachingService.Get<List<Template>>(cacheKey);

        if (cachedTemplates != null)
            return new ServiceResponse<List<Template>>($"Found {cachedTemplates.Count} records.", true,
                cachedTemplates);
        ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var templates = await ctx.Templates.ToListAsync();

            if (templates.Count > 0)
            {
                _cachingService.Set(cacheKey, templates);
                return new ServiceResponse<List<Template>>($"Found {templates.Count} records.", true, templates);
            }

            return new ServiceResponse<List<Template>>("None found.");
        }
    }

    public async Task<ServiceResponse<Template>> GetAsync(int templateId)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, templateId);
        var cachedTemplate = _cachingService.Get<Template>(cacheKey);

        if (cachedTemplate != null)
            return new ServiceResponse<Template>(FoundRecord, true, cachedTemplate);
        ;

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var result = await ctx.Templates.FindAsync(templateId);
            if (result != null)
            {
                _cachingService.Set<Template>(cacheKey, result);
                return new ServiceResponse<Template>(FoundRecord, true, result);
            }

            return new ServiceResponse<Template>("Not Found");
        }
    }

    public async Task<ServiceResponse<Template>> GetAsync(string name)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, name);
        var cachedNameTemplate = _cachingService.Get<Template>(cacheKey);

        if (cachedNameTemplate != null)
            return new ServiceResponse<Template>(FoundRecord, true, cachedNameTemplate);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var result = await ctx.Templates.FirstOrDefaultAsync(u => u.Name.ToLower() == name.ToLower());

            if (result != null)
            {
                _cachingService.Set<Template>(cacheKey, result);
                return new ServiceResponse<Template>(FoundRecord, true, result);
            }

            return new ServiceResponse<Template>("Not Found");
        }
    }

    public async Task<ServiceResponse<Template>> UpdateAsync(Template template)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var entity = await context.Templates.FindAsync(template.TemplateId);

            if (entity != null)
            {
                entity.ContentHtml = StringUtil.RestoreHrefWithJavaScript(entity.ContentHtml, template.ContentHtml);
                entity.UpdateDate = DateTime.Now;
                entity.UpdateUser = template.UpdateUser;
                context.Entry(entity).State = EntityState.Modified;
                await context.SaveChangesAsync();
                _cachingService.ClearAll();
                return new ServiceResponse<Template>($"Template record was updated.", true, template);
            }

            return new ServiceResponse<Template>($"Template with key {template.TemplateId} was not updated.");
        }
    }



    public async Task<ServiceResponse<List<Template>>> GetAllAsync(ContentType type)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, "GetAllAsyncContentType-" + type);
        var cachedTemplates = _cachingService.Get<List<Template>>(cacheKey);

        if (cachedTemplates != null)
            return new ServiceResponse<List<Template>>($"Found {cachedTemplates.Count} records.", true,
                cachedTemplates);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var templates = await ctx.Templates.Where(t => t.ContentType == type).ToListAsync();

            if (templates.Count > 0)
            {
                _cachingService.Set(cacheKey, templates);
                return new ServiceResponse<List<Template>>($"Found {templates.Count} records.", true, templates);
            }

            return new ServiceResponse<List<Template>>("None found.");
        }
    }

}


