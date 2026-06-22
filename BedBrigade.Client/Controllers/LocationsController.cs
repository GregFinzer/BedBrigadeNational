using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ILocationDataService _locationDataService;

    public LocationsController(ILocationDataService locationDataService)
    {
        _locationDataService = locationDataService ?? throw new ArgumentNullException(nameof(locationDataService));
    }

    /// <summary>
    /// Gets the locations available to the authenticated user.
    /// </summary>
    /// <returns>A list of locations visible to the current user.</returns>
    /// <response code="200">Returns the list of locations.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when an unexpected error occurs.</response>
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
        try
        {
            ServiceResponse<List<Location>> result = await _locationDataService.GetActiveLocations();
            if (!result.Success || result.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(LocationsController), nameof(GetAllAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error getting locations, try again later."));
        }
    }

    /// <summary>
    /// Gets a location by its identifier.
    /// </summary>
    /// <param name="id">The location identifier.</param>
    /// <returns>The requested location.</returns>
    /// <response code="200">Returns the requested location.</response>
    /// <response code="404">The location was not found.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when an unexpected error occurs.</response>
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
        try
        {
            ServiceResponse<Location> result = await _locationDataService.GetByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                return NotFound(CreateApiError(result.Message));
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(LocationsController), nameof(GetByIdAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error getting the location, try again later."));
        }
    }

    /// <summary>
    /// Creates a location.
    /// </summary>
    /// <param name="location">The location to create.</param>
    /// <returns>The newly created location.</returns>
    /// <response code="201">Returns the newly created location.</response>
    /// <response code="400">The location is invalid or could not be created.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when an unexpected error occurs.</response>
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
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateApiError(GetValidationMessage()));
        }

        try
        {
            ServiceResponse<Location> result = await _locationDataService.CreateAsync(location);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(CreateApiError(result.Message));
            }

            return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Data.LocationId }, result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(LocationsController), nameof(CreateAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error creating the location, try again later."));
        }
    }

    /// <summary>
    /// Updates a location.
    /// </summary>
    /// <param name="id">The location identifier.</param>
    /// <param name="location">The updated location.</param>
    /// <returns>The updated location.</returns>
    /// <response code="200">Returns the updated location.</response>
    /// <response code="400">The location is invalid or could not be updated.</response>
    /// <response code="403">A location administrator attempted to update another location.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when an unexpected error occurs.</response>
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
        if (id != location.LocationId)
        {
            return BadRequest(CreateApiError("The route id must match the location id."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateApiError(GetValidationMessage()));
        }

        if (string.Equals(_locationDataService.GetUserRole(), RoleNames.LocationAdmin,
                StringComparison.OrdinalIgnoreCase)
            && id != _locationDataService.GetUserLocationId())
        {
            return Forbid("A location administrator cannot update another location.");
        }

        try
        {
            ServiceResponse<Location> result = await _locationDataService.UpdateAsync(location);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(CreateApiError(result.Message));
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(LocationsController), nameof(UpdateAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error updating the location, try again later."));
        }
    }

    /// <summary>
    /// Deletes a location.
    /// </summary>
    /// <param name="id">The location identifier.</param>
    /// <response code="204">The location was deleted.</response>
    /// <response code="404">The location was not found or could not be deleted.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when an unexpected error occurs.</response>
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
        try
        {
            ServiceResponse<bool> result = await _locationDataService.DeleteAsync(id);
            if (!result.Success)
            {
                return NotFound(CreateApiError(result.Message));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(LocationsController), nameof(DeleteAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error deleting the location, try again later."));
        }
    }

    private static ApiError CreateApiError(string message)
    {
        return new ApiError { Message = message };
    }

    private string GetValidationMessage()
    {
        return ModelState.Values
                   .SelectMany(modelState => modelState.Errors)
                   .Select(error => error.ErrorMessage)
                   .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
               ?? "The location is invalid.";
    }
}
