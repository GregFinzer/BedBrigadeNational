using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
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
    private IConfigurationDataService _configurationDataService;

    public LocationsController(ILocationDataService locationDataService, IConfigurationDataService configurationDataService)
        : base(locationDataService, x => x.LocationId)
    {
        _configurationDataService = configurationDataService;
    }

    /// <summary>
    ///  Gets the locations available to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewLocations)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetLocations")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<Location>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<Location>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<Location>>> GetAllAsync(
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

        //This is correct that all authenticated users can see all locations, as the locations are not sensitive information.
        ServiceResponse<List<Location>> result = await DataService.GetAllAsync();
        if (!result.Success || result.Data == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
        }

        return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService, result.Data);
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


}
