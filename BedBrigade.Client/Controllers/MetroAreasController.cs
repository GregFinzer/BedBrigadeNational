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
/// Provides API endpoints for viewing and managing metro areas.
/// </summary>
public class MetroAreasController : RepositoryControllerBase<MetroArea, int, IMetroAreaDataService>
{
    public MetroAreasController(IMetroAreaDataService dataService) : base(dataService, x => x.MetroAreaId)
    {
    }

    /// <summary>
    /// Gets all metro areas.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewMetroAreas)]
    [HttpGet]
    [SwaggerOperation("GetMetroAreas")]
    public async Task<ActionResult<List<MetroArea>>> GetAllAsync() => await GetAllCoreAsync();

    /// <summary>
    /// Gets a metro area by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewMetroAreas)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetMetroArea")]
    public async Task<ActionResult<MetroArea>> GetByIdAsync(int id) => await GetByIdCoreAsync(id);

    /// <summary>
    /// Creates a metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.NationalAdmin)]
    [HttpPost]
    [SwaggerOperation("CreateMetroArea")]
    public async Task<ActionResult<MetroArea>> CreateAsync([FromBody] MetroArea metroArea) =>
        await CreateCoreAsync(metroArea);

    /// <summary>
    /// Updates a metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.NationalAdmin)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateMetroArea")]
    public async Task<ActionResult<MetroArea>> UpdateAsync(int id, [FromBody] MetroArea metroArea) =>
        await UpdateCoreAsync(id, metroArea);

    /// <summary>
    /// Deletes a metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.NationalAdmin)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteMetroArea")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteCoreAsync(id);
}
