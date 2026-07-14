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
/// Provides API endpoints for managing users within the authenticated user's location scope.
/// </summary>
public class UsersController : LocationScopedRepositoryControllerBase<User, string, IUserDataService>
{
    private readonly IConfigurationDataService _configurationDataService;

    public UsersController(IUserDataService dataService, ILocationDataService locationDataService,
        IConfigurationDataService configurationDataService)
        : base(dataService, locationDataService, x => x.UserName)
    {
        _configurationDataService = configurationDataService ?? throw new ArgumentNullException(nameof(configurationDataService));
    }

    /// <summary>
    ///  Gets the users visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewUsers)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetUsers")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<User>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<User>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<User>>> GetAllAsync(
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

        ServiceResponse<List<User>> result = await DataService.GetAllForLocationAsync(DataService.GetUserLocationId());
        if (!result.Success || result.Data == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
        }

        return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService, result.Data);
    }









}
