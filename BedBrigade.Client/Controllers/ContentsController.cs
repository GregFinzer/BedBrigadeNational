using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentsController : LocationScopedRepositoryControllerBase<Content, int, IContentDataService>
{
    public ContentsController(IContentDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.ContentId)
    {
    }

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

    [Authorize(Roles = RoleNames.CanViewPages)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetContent")]
    public async Task<ActionResult<Content>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpPost]
    [SwaggerOperation("CreateContent")]
    public async Task<ActionResult<Content>> CreateAsync([FromBody] Content content) =>
        await CreateScopedCoreAsync(content);

    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateContent")]
    public async Task<ActionResult<Content>> UpdateAsync(int id, [FromBody] Content content) =>
        await UpdateScopedCoreAsync(id, content);

    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteContent")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
