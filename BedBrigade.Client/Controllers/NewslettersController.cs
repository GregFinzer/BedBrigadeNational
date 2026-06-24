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
    [Produces("application/json")]
    [SwaggerOperation("GetNewsletters")]
    [SwaggerResponse(statusCode: 200, type: typeof(List<Newsletter>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(List<Newsletter>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<Newsletter>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a newsletter by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetNewsletter")]
    [SwaggerResponse(statusCode: 200, type: typeof(Newsletter), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The newsletter is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Newsletter not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Newsletter), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Newsletter>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a newsletter in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateNewsletter")]
    [SwaggerResponse(statusCode: 201, type: typeof(Newsletter), description: "Newsletter created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid newsletter")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Newsletter), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Newsletter>> CreateAsync([FromBody] Newsletter newsletter) =>
        await CreateScopedCoreAsync(newsletter);

    /// <summary>
    /// Updates a newsletter in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateNewsletter")]
    [SwaggerResponse(statusCode: 200, type: typeof(Newsletter), description: "Newsletter updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid newsletter")]
    [SwaggerResponse(statusCode: 403, description: "The newsletter is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Newsletter not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Newsletter), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Newsletter>> UpdateAsync(int id, [FromBody] Newsletter newsletter) =>
        await UpdateScopedCoreAsync(id, newsletter);

    /// <summary>
    /// Deletes a newsletter in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteNewsletter")]
    [SwaggerResponse(statusCode: 204, description: "Newsletter deleted")]
    [SwaggerResponse(statusCode: 403, description: "The newsletter is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Newsletter not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
