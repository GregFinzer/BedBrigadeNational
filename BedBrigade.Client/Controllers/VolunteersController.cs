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
/// Provides API endpoints for managing volunteers within the authenticated user's location scope.
/// </summary>
public class VolunteersController : LocationScopedRepositoryControllerBase<Volunteer, int, IVolunteerDataService>
{
    public VolunteersController(IVolunteerDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.VolunteerId)
    {
    }

    /// <summary>
    /// Gets the volunteers visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewVolunteers)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetVolunteers")]
    [SwaggerResponse(statusCode: 200, type: typeof(List<Volunteer>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(List<Volunteer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<Volunteer>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a volunteer by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewVolunteers)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetVolunteer")]
    [SwaggerResponse(statusCode: 200, type: typeof(Volunteer), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The volunteer is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Volunteer not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Volunteer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Volunteer>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a volunteer in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateVolunteer")]
    [SwaggerResponse(statusCode: 201, type: typeof(Volunteer), description: "Volunteer created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid volunteer")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Volunteer), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Volunteer>> CreateAsync([FromBody] Volunteer volunteer) =>
        await CreateScopedCoreAsync(volunteer);

    /// <summary>
    /// Updates a volunteer in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateVolunteer")]
    [SwaggerResponse(statusCode: 200, type: typeof(Volunteer), description: "Volunteer updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid volunteer")]
    [SwaggerResponse(statusCode: 403, description: "The volunteer is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Volunteer not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Volunteer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Volunteer>> UpdateAsync(int id, [FromBody] Volunteer volunteer) =>
        await UpdateScopedCoreAsync(id, volunteer);

    /// <summary>
    /// Deletes a volunteer in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteVolunteer")]
    [SwaggerResponse(statusCode: 204, description: "Volunteer deleted")]
    [SwaggerResponse(statusCode: 403, description: "The volunteer is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Volunteer not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
