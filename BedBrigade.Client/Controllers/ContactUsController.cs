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
/// Provides API endpoints for managing contact-us requests within the authenticated user's location scope.
/// </summary>
public class ContactUsController : LocationScopedRepositoryControllerBase<ContactUs, int, IContactUsDataService>
{
    private readonly IConfigurationDataService _configurationDataService;

    public ContactUsController(IContactUsDataService dataService, ILocationDataService locationDataService,
        IConfigurationDataService configurationDataService)
        : base(dataService, locationDataService, x => x.ContactUsId)
    {
        _configurationDataService = configurationDataService ?? throw new ArgumentNullException(nameof(configurationDataService));
    }

    /// <summary>
    /// Gets the contact-us requests visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewContacts)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetContactUs")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<ContactUs>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<ContactUs>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<ContactUs>>> GetAllAsync(
        [FromQuery] int pageNumber,
        [FromQuery] int itemsPerPage) =>
        await GetScopedPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService);

    /// <summary>
    /// Gets a contact-us request by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewContacts)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetContactUsById")]
    [SwaggerResponse(statusCode: 200, type: typeof(ContactUs), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The contact-us request is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Contact-us request not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(ContactUs), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContactUs>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a contact-us request in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageContacts)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateContactUs")]
    [SwaggerResponse(statusCode: 201, type: typeof(ContactUs), description: "Contact-us request created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid contact-us request")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(ContactUs), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContactUs>> CreateAsync([FromBody] ContactUs contactUs) =>
        await CreateScopedCoreAsync(contactUs);

    /// <summary>
    /// Updates a contact-us request in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageContacts)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateContactUs")]
    [SwaggerResponse(statusCode: 200, type: typeof(ContactUs), description: "Contact-us request updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid contact-us request")]
    [SwaggerResponse(statusCode: 403, description: "The contact-us request is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Contact-us request not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(ContactUs), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContactUs>> UpdateAsync(int id, [FromBody] ContactUs contactUs) =>
        await UpdateScopedCoreAsync(id, contactUs);

    /// <summary>
    /// Deletes a contact-us request in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageContacts)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteContactUs")]
    [SwaggerResponse(statusCode: 204, description: "Contact-us request deleted")]
    [SwaggerResponse(statusCode: 403, description: "The contact-us request is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Contact-us request not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
