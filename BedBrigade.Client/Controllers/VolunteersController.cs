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
    [SwaggerOperation("GetVolunteers")]
    public async Task<ActionResult<List<Volunteer>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a volunteer by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewVolunteers)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetVolunteer")]
    public async Task<ActionResult<Volunteer>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a volunteer in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpPost]
    [SwaggerOperation("CreateVolunteer")]
    public async Task<ActionResult<Volunteer>> CreateAsync([FromBody] Volunteer volunteer) =>
        await CreateScopedCoreAsync(volunteer);

    /// <summary>
    /// Updates a volunteer in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateVolunteer")]
    public async Task<ActionResult<Volunteer>> UpdateAsync(int id, [FromBody] Volunteer volunteer) =>
        await UpdateScopedCoreAsync(id, volunteer);

    /// <summary>
    /// Deletes a volunteer in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteVolunteer")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
