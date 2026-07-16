using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
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
        int maxItemsPerPage = await _configurationDataService.GetConfigValueAsIntAsync(
            ConfigSection.System, ConfigNames.MaxItemsPerPage);
        ActionResult? validationResult = ValidatePagingParameters(pageNumber, itemsPerPage, maxItemsPerPage);
        if (validationResult != null)
        {
            return validationResult;
        }

        ServiceResponse<List<Content>> result;
        if (DataService.IsUserNationalAdmin())
        {
            result = await DataService.GetAllExceptBlogTypes();
        }
        else
        {
            result = await DataService.GetForLocationExceptBlogTypes(DataService.GetUserLocationId());
        }

        if (!result.Success || result.Data == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
        }

        return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService, result.Data);
    }

    /// <summary>
    /// Gets paged stories for the authenticated user's location.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewPages)]
    [HttpGet("stories")]
    [Produces("application/json")]
    [SwaggerOperation("GetStories")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<BlogItem>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid paging parameters")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<BlogItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<BlogItem>>> GetStories(
        [FromQuery] int pageNumber,
        [FromQuery] int itemsPerPage)
    {
        try
        {
            int maxItemsPerPage = await _configurationDataService.GetConfigValueAsIntAsync(
                ConfigSection.System, ConfigNames.MaxItemsPerPage);
            ActionResult? validationResult = ValidatePagingParameters(pageNumber, itemsPerPage, maxItemsPerPage);
            if (validationResult != null)
            {
                return validationResult;
            }

            ServiceResponse<List<BlogItem>> result = await DataService.GetBlogItems(DataService.GetUserLocationId(), ContentType.Stories);
            if (!result.Success || result.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
            }

            return Ok(CreatePageResponse(result.Data, pageNumber, itemsPerPage, maxItemsPerPage));
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", GetType().Name, nameof(GetStories));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error getting stories, try again later."));
        }
    }

    /// <summary>
    /// Gets paged stories for the authenticated user's location.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewPages)]
    [HttpGet("news")]
    [Produces("application/json")]
    [SwaggerOperation("GetNews")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<BlogItem>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid paging parameters")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<BlogItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<BlogItem>>> GetNews(
        [FromQuery] int pageNumber,
        [FromQuery] int itemsPerPage)
    {
        try
        {
            int maxItemsPerPage = await _configurationDataService.GetConfigValueAsIntAsync(
                ConfigSection.System, ConfigNames.MaxItemsPerPage);
            ActionResult? validationResult = ValidatePagingParameters(pageNumber, itemsPerPage, maxItemsPerPage);
            if (validationResult != null)
            {
                return validationResult;
            }

            ServiceResponse<List<BlogItem>> result = await DataService.GetBlogItems(DataService.GetUserLocationId(), ContentType.News);
            if (!result.Success || result.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
            }

            return Ok(CreatePageResponse(result.Data, pageNumber, itemsPerPage, maxItemsPerPage));
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", GetType().Name, nameof(GetNews));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error getting news, try again later."));
        }
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

    private static ActionResult? ValidatePagingParameters(int pageNumber, int itemsPerPage, int maxItemsPerPage)
    {
        if (pageNumber < 1)
        {
            return new BadRequestObjectResult(CreateApiError("pageNumber must be greater than or equal to 1."));
        }

        if (itemsPerPage < 1 || itemsPerPage > maxItemsPerPage)
        {
            return new BadRequestObjectResult(
                CreateApiError($"itemsPerPage must be between 1 and {maxItemsPerPage}."));
        }

        return null;
    }

    private static PageResponse<BlogItem> CreatePageResponse(List<BlogItem> entities,
        int pageNumber, int itemsPerPage, int maxItemsPerPage)
    {
        int normalizedPageNumber = Math.Max(pageNumber, 1);
        int normalizedItemsPerPage = Math.Clamp(itemsPerPage, 1, maxItemsPerPage);
        int numberOfItems = entities.Count;
        int maxPage = numberOfItems == 0
            ? 0
            : (int)Math.Ceiling(numberOfItems / (double)normalizedItemsPerPage);
        long itemsToSkip = ((long)normalizedPageNumber - 1) * normalizedItemsPerPage;
        List<BlogItem> pageItems = itemsToSkip > int.MaxValue
            ? []
            : entities
                .Skip((int)itemsToSkip)
                .Take(normalizedItemsPerPage)
                .ToList();

        return new PageResponse<BlogItem>
        {
            PageNumber = normalizedPageNumber,
            MaxPage = maxPage,
            NumberOfItems = numberOfItems,
            ItemsPerPage = normalizedItemsPerPage,
            Items = pageItems
        };
    }
}
