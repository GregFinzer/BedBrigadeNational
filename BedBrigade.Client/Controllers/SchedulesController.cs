using System.Net;
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
/// Provides API endpoints for managing schedules within the authenticated user's location scope.
/// </summary>
public class SchedulesController : LocationScopedRepositoryControllerBase<Schedule, int, IScheduleDataService>
{
    private readonly IConfigurationDataService _configurationDataService;

    public SchedulesController(IScheduleDataService dataService, ILocationDataService locationDataService,
        IConfigurationDataService configurationDataService)
        : base(dataService, locationDataService, x => x.ScheduleId)
    {
        _configurationDataService = configurationDataService ?? throw new ArgumentNullException(nameof(configurationDataService));
    }




    /// <summary>
    ///  Gets the schedules visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewSchedule)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetSchedules")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<Schedule>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<Schedule>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<Schedule>>> GetAllAsync(
        [FromQuery] int pageNumber,
        [FromQuery] int itemsPerPage,
        [FromQuery] TimePeriod timePeriod)
    {
        int maxItemsPerPage = await _configurationDataService.GetConfigValueAsIntAsync(
            ConfigSection.System, ConfigNames.MaxItemsPerPage);
        ActionResult? validationResult = ValidatePagingParameters(pageNumber, itemsPerPage, maxItemsPerPage);
        if (validationResult != null)
        {
            return validationResult;
        }

        ServiceResponse<List<Schedule>> result;
        switch (timePeriod)
        {
            case TimePeriod.All:
                result = await DataService.GetSchedulesByLocationId(DataService.GetUserLocationId());
                break;
            case TimePeriod.Future:
                result = await DataService.GetFutureSchedulesByLocationId(DataService.GetUserLocationId());
                break;
            case TimePeriod.Past:
                result = await DataService.GetPastSchedulesByLocationId(DataService.GetUserLocationId());
                break;
            default:
                return StatusCode(StatusCodes.Status400BadRequest, CreateApiError("Invalid Time Period"));
        }

        if (!result.Success || result.Data == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
        }

        return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService, result.Data);
    }

    /// <summary>
    /// Gets a schedule by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewSchedule)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetSchedule")]
    [SwaggerResponse(statusCode: 200, type: typeof(Schedule), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The schedule is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Schedule not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Schedule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Schedule>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a schedule in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateSchedule")]
    [SwaggerResponse(statusCode: 201, type: typeof(Schedule), description: "Schedule created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid schedule")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Schedule), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Schedule>> CreateAsync([FromBody] Schedule schedule) =>
        await CreateScopedCoreAsync(schedule);

    /// <summary>
    /// Updates a schedule in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateSchedule")]
    [SwaggerResponse(statusCode: 200, type: typeof(Schedule), description: "Schedule updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid schedule")]
    [SwaggerResponse(statusCode: 403, description: "The schedule is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Schedule not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(Schedule), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Schedule>> UpdateAsync(int id, [FromBody] Schedule schedule) =>
        await UpdateScopedCoreAsync(id, schedule);

    /// <summary>
    /// Deletes a schedule in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteSchedule")]
    [SwaggerResponse(statusCode: 204, description: "Schedule deleted")]
    [SwaggerResponse(statusCode: 403, description: "The schedule is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Schedule not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
