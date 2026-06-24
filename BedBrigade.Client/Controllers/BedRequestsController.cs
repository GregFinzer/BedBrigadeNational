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
public class BedRequestsController : ControllerBase
{
    private readonly IBedRequestDataService _bedRequestDataService;
    private readonly ILocationDataService _locationDataService;

    public BedRequestsController(IBedRequestDataService bedRequestDataService,
        ILocationDataService locationDataService)
    {
        _bedRequestDataService =
            bedRequestDataService ?? throw new ArgumentNullException(nameof(bedRequestDataService));
        _locationDataService = locationDataService ?? throw new ArgumentNullException(nameof(locationDataService));
    }

    /// <summary>
    /// Gets the bed requests for the authenticated user's location or metro area.
    /// </summary>
    /// <returns>A list of bed requests visible to the current user.</returns>
    /// <response code="200">Returns the list of bed requests.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when the bed requests cannot be loaded.</response>
    [Authorize(Roles = RoleNames.CanViewBedRequests)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetBedRequests")]
    [SwaggerResponse(statusCode: 200, type: typeof(List<BedRequest>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(List<BedRequest>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<BedRequest>>> GetBedRequests()
    {
        try
        {
            ServiceResponse<LocationScope> scopeResult = await GetLocationScope();
            if (!scopeResult.Success || scopeResult.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    CreateApiError(scopeResult.Message));
            }

            ServiceResponse<List<BedRequest>> bedRequestsResult =
                await _bedRequestDataService.LoadBedRequests(scopeResult.Data.UserLocation,
                    scopeResult.Data.MetroLocations);

            if (!bedRequestsResult.Success || bedRequestsResult.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    CreateApiError(bedRequestsResult.Message));
            }

            return Ok(bedRequestsResult.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(BedRequestsController),
                nameof(GetBedRequests));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error getting bed requests, try again later."));
        }
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
        try
        {
            ServiceResponse<BedRequest> result = await _bedRequestDataService.GetByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                return NotFound(CreateApiError(result.Message));
            }

            ServiceResponse<LocationScope> scopeResult = await GetLocationScope();
            if (!scopeResult.Success || scopeResult.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    CreateApiError(scopeResult.Message));
            }

            if (!scopeResult.Data.LocationIds.Contains(result.Data.LocationId))
            {
                return Forbid();
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(BedRequestsController),
                nameof(GetByIdAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error getting the bed request, try again later."));
        }
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
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateApiError(GetValidationMessage()));
        }

        try
        {
            ServiceResponse<LocationScope> scopeResult = await GetLocationScope();
            if (!scopeResult.Success || scopeResult.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    CreateApiError(scopeResult.Message));
            }

            if (!scopeResult.Data.LocationIds.Contains(bedRequest.LocationId))
            {
                return Forbid();
            }

            ServiceResponse<BedRequest> result = await _bedRequestDataService.CreateAsync(bedRequest);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(CreateApiError(result.Message));
            }

            return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Data.BedRequestId }, result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(BedRequestsController),
                nameof(CreateAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error creating the bed request, try again later."));
        }
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
        if (id != bedRequest.BedRequestId)
        {
            return BadRequest(CreateApiError("The route id must match the bed request id."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateApiError(GetValidationMessage()));
        }

        try
        {
            ServiceResponse<BedRequest> existingResult = await _bedRequestDataService.GetByIdAsync(id);
            if (!existingResult.Success || existingResult.Data == null)
            {
                return NotFound(CreateApiError(existingResult.Message));
            }

            ServiceResponse<LocationScope> scopeResult = await GetLocationScope();
            if (!scopeResult.Success || scopeResult.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    CreateApiError(scopeResult.Message));
            }

            if (!scopeResult.Data.LocationIds.Contains(existingResult.Data.LocationId)
                || !scopeResult.Data.LocationIds.Contains(bedRequest.LocationId))
            {
                return Forbid();
            }

            ServiceResponse<BedRequest> result = await _bedRequestDataService.UpdateAsync(bedRequest);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(CreateApiError(result.Message));
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(BedRequestsController),
                nameof(UpdateAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error updating the bed request, try again later."));
        }
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
        try
        {
            ServiceResponse<BedRequest> existingResult = await _bedRequestDataService.GetByIdAsync(id);
            if (!existingResult.Success || existingResult.Data == null)
            {
                return NotFound(CreateApiError(existingResult.Message));
            }

            ServiceResponse<LocationScope> scopeResult = await GetLocationScope();
            if (!scopeResult.Success || scopeResult.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    CreateApiError(scopeResult.Message));
            }

            if (!scopeResult.Data.LocationIds.Contains(existingResult.Data.LocationId))
            {
                return Forbid();
            }

            ServiceResponse<bool> result = await _bedRequestDataService.DeleteAsync(id);
            if (!result.Success)
            {
                return NotFound(CreateApiError(result.Message));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(BedRequestsController),
                nameof(DeleteAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error deleting the bed request, try again later."));
        }
    }

    private async Task<ServiceResponse<LocationScope>> GetLocationScope()
    {
        int userLocationId = _bedRequestDataService.GetUserLocationId();
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
               ?? "The bed request is invalid.";
    }

    private sealed record LocationScope(Location UserLocation, List<Location>? MetroLocations,
        HashSet<int> LocationIds);
}
