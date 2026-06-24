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
    [Produces("application/json")]
    [SwaggerOperation("GetMetroAreas")]
    [SwaggerResponse(statusCode: 200, type: typeof(List<MetroArea>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(List<MetroArea>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<MetroArea>>> GetAllAsync() => await GetAllCoreAsync();

    /// <summary>
    /// Gets a metro area by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewMetroAreas)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetMetroArea")]
    [SwaggerResponse(statusCode: 200, type: typeof(MetroArea), description: "Successful operation")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Metro area not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(MetroArea), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MetroArea>> GetByIdAsync(int id) => await GetByIdCoreAsync(id);

    /// <summary>
    /// Creates a metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.NationalAdmin)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateMetroArea")]
    [SwaggerResponse(statusCode: 201, type: typeof(MetroArea), description: "Metro area created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid metro area")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(MetroArea), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MetroArea>> CreateAsync([FromBody] MetroArea metroArea) =>
        await CreateCoreAsync(metroArea);

    /// <summary>
    /// Updates a metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.NationalAdmin)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateMetroArea")]
    [SwaggerResponse(statusCode: 200, type: typeof(MetroArea), description: "Metro area updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid metro area")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(MetroArea), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MetroArea>> UpdateAsync(int id, [FromBody] MetroArea metroArea) =>
        await UpdateCoreAsync(id, metroArea);

    /// <summary>
    /// Deletes a metro area.
    /// </summary>
    [Authorize(Roles = RoleNames.NationalAdmin)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteMetroArea")]
    [SwaggerResponse(statusCode: 204, description: "Metro area deleted")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Metro area not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteCoreAsync(id);
}
