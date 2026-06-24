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
/// Provides API endpoints for managing newsletters within the authenticated user's location scope.
/// </summary>
public class NewslettersController : LocationScopedRepositoryControllerBase<Newsletter, int, INewsletterDataService>
{
    public NewslettersController(INewsletterDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.NewsletterId)
    {
    }

    /// <summary>
    /// Gets the newsletters visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpGet]
    [SwaggerOperation("GetNewsletters")]
    public async Task<ActionResult<List<Newsletter>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a newsletter by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetNewsletter")]
    public async Task<ActionResult<Newsletter>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a newsletter in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpPost]
    [SwaggerOperation("CreateNewsletter")]
    public async Task<ActionResult<Newsletter>> CreateAsync([FromBody] Newsletter newsletter) =>
        await CreateScopedCoreAsync(newsletter);

    /// <summary>
    /// Updates a newsletter in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateNewsletter")]
    public async Task<ActionResult<Newsletter>> UpdateAsync(int id, [FromBody] Newsletter newsletter) =>
        await UpdateScopedCoreAsync(id, newsletter);

    /// <summary>
    /// Deletes a newsletter in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteNewsletter")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
