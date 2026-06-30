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
/// Provides API endpoints for managing bed requests within the authenticated user's location or metro-area scope.
/// </summary>
public class BedRequestsController
    : LocationScopedRepositoryControllerBase<BedRequest, int, IBedRequestDataService>
{
    private readonly ILocationDataService _locationDataService;
    private readonly IConfigurationDataService _configurationDataService;
    
    public BedRequestsController(IBedRequestDataService bedRequestDataService,
        ILocationDataService locationDataService,
        IConfigurationDataService configurationDataService) : base(bedRequestDataService, locationDataService,
        x => x.BedRequestId)
    {
        _locationDataService = locationDataService ?? throw new ArgumentNullException(nameof(locationDataService));
        _configurationDataService = configurationDataService  ?? throw new ArgumentNullException(nameof(configurationDataService));
    }

    /// <summary>
    /// Gets the bed requests for the authenticated user's location or metro area.
    /// </summary>
    /// <returns>A page of bed requests visible to the current user.</returns>
    /// <response code="200">Returns a page of bed requests.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when the bed requests cannot be loaded.</response>
    [Authorize(Roles = RoleNames.CanViewBedRequests)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetBedRequests")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<BedRequest>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<BedRequest>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<BedRequest>>> GetBedRequests(
        [FromQuery] int pageNumber,
        [FromQuery] int itemsPerPage)
    {
        return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService, async () =>
        {
            ServiceResponse<LocationScope> scopeResult = await GetLocationScope();
            if (!scopeResult.Success || scopeResult.Data == null)
            {
                return new ServiceResponse<List<BedRequest>>(scopeResult.Message);
            }

            return await DataService.LoadBedRequests(scopeResult.Data.UserLocation,
                scopeResult.Data.MetroLocations);
        });
    }

    /// <summary>
    /// Gets a bed request by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewBedRequests)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetBedRequest")]
    [SwaggerResponse(statusCode: 200, type: typeof(BedRequest), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The bed request is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Bed request not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(BedRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BedRequest>> GetByIdAsync(int id)
    {
        return await GetScopedByIdCoreAsync(id);
    }

    /// <summary>
    /// Creates a bed request in the authenticated user's location or metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageBedRequests)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateBedRequest")]
    [SwaggerResponse(statusCode: 201, type: typeof(BedRequest), description: "Bed request created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid bed request")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(BedRequest), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BedRequest>> CreateAsync([FromBody] BedRequest bedRequest)
    {
        return await CreateScopedCoreAsync(bedRequest);
    }

    /// <summary>
    /// Updates a bed request in the authenticated user's location or metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageBedRequests)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateBedRequest")]
    [SwaggerResponse(statusCode: 200, type: typeof(BedRequest), description: "Bed request updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid bed request")]
    [SwaggerResponse(statusCode: 403, description: "The bed request is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Bed request not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(BedRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BedRequest>> UpdateAsync(int id, [FromBody] BedRequest bedRequest)
    {
        return await UpdateScopedCoreAsync(id, bedRequest);
    }

    /// <summary>
    /// Deletes a bed request in the authenticated user's location or metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageBedRequests)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteBedRequest")]
    [SwaggerResponse(statusCode: 204, description: "Bed request deleted")]
    [SwaggerResponse(statusCode: 403, description: "The bed request is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Bed request not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        return await DeleteScopedCoreAsync(id);
    }

    private async Task<ServiceResponse<LocationScope>> GetLocationScope()
    {
        int userLocationId = DataService.GetUserLocationId();
        ServiceResponse<Location> userLocationResult =
            await _locationDataService.GetByIdAsync(userLocationId);

        if (!userLocationResult.Success || userLocationResult.Data == null)
        {
            return new ServiceResponse<LocationScope>(userLocationResult.Message);
        }

        Location userLocation = userLocationResult.Data;
        List<Location>? metroLocations = null;

        if (userLocation.IsMetroLocation() && userLocation.MetroAreaId.HasValue)
        {
            ServiceResponse<List<Location>> metroLocationsResult =
                await _locationDataService.GetLocationsByMetroAreaId(userLocation.MetroAreaId.Value);

            if (!metroLocationsResult.Success || metroLocationsResult.Data == null)
            {
                return new ServiceResponse<LocationScope>(metroLocationsResult.Message);
            }

            metroLocations = metroLocationsResult.Data;
        }

        HashSet<int> locationIds = metroLocations?.Select(location => location.LocationId).ToHashSet()
                                   ?? [userLocation.LocationId];

        return new ServiceResponse<LocationScope>("Found user location scope", true,
            new LocationScope(userLocation, metroLocations, locationIds));
    }

    protected override string GetEntityDisplayName()
    {
        return "bed request";
    }

    private sealed record LocationScope(Location UserLocation, List<Location>? MetroLocations,
        HashSet<int> LocationIds);
}
