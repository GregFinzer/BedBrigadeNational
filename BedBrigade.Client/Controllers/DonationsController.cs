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
/// Provides API endpoints for managing donations within the authenticated user's location scope.
/// </summary>
public class DonationsController : LocationScopedRepositoryControllerBase<Donation, int, IDonationDataService>
{
    private readonly IConfigurationDataService _configurationDataService;

    public DonationsController(IDonationDataService dataService, ILocationDataService locationDataService,
        IConfigurationDataService configurationDataService)
        : base(dataService, locationDataService, x => x.DonationId)
    {
        _configurationDataService = configurationDataService ?? throw new ArgumentNullException(nameof(configurationDataService));
    }

    /// <summary>
    /// Gets the donations visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetDonations")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<Donation>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<Donation>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<Donation>>> GetAllAsync(
        [FromQuery] int pageNumber,
        [FromQuery] int itemsPerPage) =>
        await GetScopedPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService);

    /// <summary>
    /// Gets a donation by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetDonation")]
    [SwaggerResponse(statusCode: 200, type: typeof(Donation), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The donation is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Donation not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Donation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Donation>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a donation in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateDonation")]
    [SwaggerResponse(statusCode: 201, type: typeof(Donation), description: "Donation created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid donation")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Donation), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Donation>> CreateAsync([FromBody] Donation donation) =>
        await CreateScopedCoreAsync(donation);

    /// <summary>
    /// Updates a donation in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateDonation")]
    [SwaggerResponse(statusCode: 200, type: typeof(Donation), description: "Donation updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid donation")]
    [SwaggerResponse(statusCode: 403, description: "The donation is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Donation not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Donation), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Donation>> UpdateAsync(int id, [FromBody] Donation donation) =>
        await UpdateScopedCoreAsync(id, donation);

    /// <summary>
    /// Deletes a donation in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteDonation")]
    [SwaggerResponse(statusCode: 204, description: "Donation deleted")]
    [SwaggerResponse(statusCode: 403, description: "The donation is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Donation not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
