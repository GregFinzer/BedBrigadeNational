using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VolunteersController : LocationScopedRepositoryControllerBase<Volunteer, int, IVolunteerDataService>
{
    public VolunteersController(IVolunteerDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.VolunteerId)
    {
    }

    [Authorize(Roles = RoleNames.CanViewVolunteers)]
    [HttpGet]
    [SwaggerOperation("GetVolunteers")]
    public async Task<ActionResult<List<Volunteer>>> GetAllAsync() => await GetScopedAllCoreAsync();

    [Authorize(Roles = RoleNames.CanViewVolunteers)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetVolunteer")]
    public async Task<ActionResult<Volunteer>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpPost]
    [SwaggerOperation("CreateVolunteer")]
    public async Task<ActionResult<Volunteer>> CreateAsync([FromBody] Volunteer volunteer) =>
        await CreateScopedCoreAsync(volunteer);

    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateVolunteer")]
    public async Task<ActionResult<Volunteer>> UpdateAsync(int id, [FromBody] Volunteer volunteer) =>
        await UpdateScopedCoreAsync(id, volunteer);

    [Authorize(Roles = RoleNames.CanManageVolunteers)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteVolunteer")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
