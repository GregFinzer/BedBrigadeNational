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
/// Provides API endpoints for viewing and managing Bed Brigade locations.
/// </summary>
public class LocationsController : RepositoryControllerBase<Location, int, ILocationDataService>
{
    public LocationsController(ILocationDataService locationDataService)
        : base(locationDataService, x => x.LocationId)
    {
    }

    /// <summary>
    /// Gets the locations available to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewLocations)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetLocations")]
    [SwaggerResponse(statusCode: 200, type: typeof(List<Location>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(List<Location>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<Location>>> GetAllAsync()
    {
        return await GetAllCoreAsync(() => DataService.GetActiveLocations(), "locations");
    }

    /// <summary>
    /// Gets a location by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewLocations)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetLocation")]
    [SwaggerResponse(statusCode: 200, type: typeof(Location), description: "Successful operation")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Location not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Location), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Location>> GetByIdAsync(int id)
    {
        return await GetByIdCoreAsync(id);
    }

    /// <summary>
    /// Creates a location.
    /// </summary>
    [Authorize(Roles = RoleNames.NationalAdmin)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateLocation")]
    [SwaggerResponse(statusCode: 201, type: typeof(Location), description: "Location created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid location")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Location), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Location>> CreateAsync([FromBody] Location location)
    {
        return await CreateCoreAsync(location);
    }

    /// <summary>
    /// Updates a location.
    /// </summary>
    [Authorize(Roles = $"{RoleNames.NationalAdmin}, {RoleNames.LocationAdmin}")]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateLocation")]
    [SwaggerResponse(statusCode: 200, type: typeof(Location), description: "Location updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid location")]
    [SwaggerResponse(statusCode: 403, description: "The user cannot update this location")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Location), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Location>> UpdateAsync(int id, [FromBody] Location location)
    {
        if (string.Equals(DataService.GetUserRole(), RoleNames.LocationAdmin, StringComparison.OrdinalIgnoreCase)
            && id != DataService.GetUserLocationId())
        {
            return Forbid("A location administrator cannot update another location.");
        }

        return await UpdateCoreAsync(id, location);
    }

    /// <summary>
    /// Deletes a location.
    /// </summary>
    [Authorize(Roles = RoleNames.NationalAdmin)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteLocation")]
    [SwaggerResponse(statusCode: 204, description: "Location deleted")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Location not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        return await DeleteCoreAsync(id);
    }
}
