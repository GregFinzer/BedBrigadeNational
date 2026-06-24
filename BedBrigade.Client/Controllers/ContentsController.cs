using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Provides API endpoints for managing site content and pages within the authenticated user's location scope.
/// </summary>
public class ContentsController : LocationScopedRepositoryControllerBase<Content, int, IContentDataService>
{
    public ContentsController(IContentDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.ContentId)
    {
    }

    /// <summary>
    /// Gets non-blog content visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewPages)]
    [HttpGet]
    [SwaggerOperation("GetContents")]
    public async Task<ActionResult<List<Content>>> GetAllAsync()
    {
        if (DataService.IsUserNationalAdmin())
        {
            return await GetAllCoreAsync(() => DataService.GetAllExceptBlogTypes());
        }

        return await GetAllCoreAsync(() => DataService.GetForLocationExceptBlogTypes(DataService.GetUserLocationId()));
    }

    /// <summary>
    /// Gets a content item by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewPages)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetContent")]
    public async Task<ActionResult<Content>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a content item in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpPost]
    [SwaggerOperation("CreateContent")]
    public async Task<ActionResult<Content>> CreateAsync([FromBody] Content content) =>
        await CreateScopedCoreAsync(content);

    /// <summary>
    /// Updates a content item in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateContent")]
    public async Task<ActionResult<Content>> UpdateAsync(int id, [FromBody] Content content) =>
        await UpdateScopedCoreAsync(id, content);

    /// <summary>
    /// Deletes a content item in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteContent")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
