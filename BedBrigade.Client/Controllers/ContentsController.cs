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
    private readonly IConfigurationDataService _configurationDataService;

    public ContentsController(IContentDataService dataService, ILocationDataService locationDataService,
        IConfigurationDataService configurationDataService)
        : base(dataService, locationDataService, x => x.ContentId)
    {
        _configurationDataService = configurationDataService ?? throw new ArgumentNullException(nameof(configurationDataService));
    }

    /// <summary>
    /// Gets non-blog content visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewPages)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetContents")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<Content>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<Content>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<Content>>> GetAllAsync(
        [FromQuery] int pageNumber,
        [FromQuery] int itemsPerPage)
    {
        if (DataService.IsUserNationalAdmin())
        {
            return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService,
                () => DataService.GetAllExceptBlogTypes());
        }

        return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService,
            () => DataService.GetForLocationExceptBlogTypes(DataService.GetUserLocationId()));
    }

    /// <summary>
    /// Gets a content item by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewPages)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetContent")]
    [SwaggerResponse(statusCode: 200, type: typeof(Content), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The content item is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Content not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Content), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Content>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a content item in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateContent")]
    [SwaggerResponse(statusCode: 201, type: typeof(Content), description: "Content created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid content item")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Content), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Content>> CreateAsync([FromBody] Content content) =>
        await CreateScopedCoreAsync(content);

    /// <summary>
    /// Updates a content item in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateContent")]
    [SwaggerResponse(statusCode: 200, type: typeof(Content), description: "Content updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid content item")]
    [SwaggerResponse(statusCode: 403, description: "The content item is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Content not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Content), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Content>> UpdateAsync(int id, [FromBody] Content content) =>
        await UpdateScopedCoreAsync(id, content);

    /// <summary>
    /// Deletes a content item in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManagePages)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteContent")]
    [SwaggerResponse(statusCode: 204, description: "Content deleted")]
    [SwaggerResponse(statusCode: 403, description: "The content item is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Content not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
