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
/// Provides API endpoints for managing volunteer sign-ups within the authenticated user's location scope.
/// </summary>
public class SignUpsController : LocationScopedRepositoryControllerBase<SignUp, int, ISignUpDataService>
{
    private readonly IConfigurationDataService _configurationDataService;

    public SignUpsController(ISignUpDataService dataService, ILocationDataService locationDataService,
        IConfigurationDataService configurationDataService)
        : base(dataService, locationDataService, x => x.SignUpId)
    {
        _configurationDataService = configurationDataService ?? throw new ArgumentNullException(nameof(configurationDataService));
    }

    /// <summary>
    ///  Gets the signups visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewSignUps)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetSignUps")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<SignUp>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<SignUp>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<SignUp>>> GetAllAsync(
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

        ServiceResponse<List<SignUp>> result = await DataService.GetSignUpsForDashboard(DataService.GetUserLocationId());

        if (!result.Success || result.Data == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
        }

        switch (timePeriod)
        {
            case TimePeriod.All:
                break;
            case TimePeriod.Future:
                result = result.Data.Where(s => s.Si.ScheduleDate >= DateTime.Today).ToList();
                break;
            case TimePeriod.Past:
                result = await DataService.GetPastSchedulesByLocationId(DataService.GetUserLocationId());
                break;
            default:
                return StatusCode(StatusCodes.Status400BadRequest, CreateApiError("Invalid Time Period"));
        }



        return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService, result.Data);
    }

    /// <summary>
    /// Gets the sign-ups visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewSignUps)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetSignUps")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<SignUp>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<SignUp>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<SignUp>>> GetAllAsync(
        [FromQuery] int pageNumber,
        [FromQuery] int itemsPerPage) =>
        await GetScopedPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService);

    /// <summary>
    /// Gets a sign-up by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewSignUps)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetSignUp")]
    [SwaggerResponse(statusCode: 200, type: typeof(SignUp), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The sign-up is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Sign-up not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(SignUp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SignUp>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a sign-up in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateSignUp")]
    [SwaggerResponse(statusCode: 201, type: typeof(SignUp), description: "Sign-up created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid sign-up")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(SignUp), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SignUp>> CreateAsync([FromBody] SignUp signUp) =>
        await CreateScopedCoreAsync(signUp);

    /// <summary>
    /// Updates a sign-up in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateSignUp")]
    [SwaggerResponse(statusCode: 200, type: typeof(SignUp), description: "Sign-up updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid sign-up")]
    [SwaggerResponse(statusCode: 403, description: "The sign-up is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Sign-up not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(SignUp), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SignUp>> UpdateAsync(int id, [FromBody] SignUp signUp) =>
        await UpdateScopedCoreAsync(id, signUp);

    /// <summary>
    /// Deletes a sign-up in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteSignUp")]
    [SwaggerResponse(statusCode: 204, description: "Sign-up deleted")]
    [SwaggerResponse(statusCode: 403, description: "The sign-up is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Sign-up not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
