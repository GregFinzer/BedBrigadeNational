using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Data.Services;

public class TemplateDataService : Repository<Template>, ITemplateDataService
{
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;

    public TemplateDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, AuthenticationStateProvider authProvider) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
    }

    public async Task<ServiceResponse<Template>> GetByNameAsync(string name)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetByNameAsync ({name})");
        var cachedTemplate = _cachingService.Get<Template>(cacheKey);

        if (cachedTemplate != null)
            return new ServiceResponse<Template>($"Found {name} in cache.", true, cachedTemplate);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var template = await ctx.Templates.FirstOrDefaultAsync(t => t.Name == name);

            if (template != null)
            {
                _cachingService.Set(cacheKey, template);
                return new ServiceResponse<Template>($"Found {name}.", true, template);
            }

            return new ServiceResponse<Template>("None found.");
        }
    }

    public async Task<ServiceResponse<List<Template>>> GetByContentTypeAsync(ContentType type)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetByContentTypeAsync ({type})");
        var cachedTemplates = _cachingService.Get<List<Template>>(cacheKey);

        if (cachedTemplates != null)
            return new ServiceResponse<List<Template>>($"Found {cachedTemplates.Count} records in cache", true, cachedTemplates);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var templates = await ctx.Templates.Where(t => t.ContentType == type).ToListAsync();

            if (templates != null)
            {
                _cachingService.Set(cacheKey, templates);
                return new ServiceResponse<List<Template>>($"Found {templates.Count} records.", true, templates);
            }

            return new ServiceResponse<List<Template>>("None found.");
        }   
    }

}


