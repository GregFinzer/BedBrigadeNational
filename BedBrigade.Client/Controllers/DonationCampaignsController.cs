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
/// Provides API endpoints for managing donation campaigns within the authenticated user's location scope.
/// </summary>
public class DonationCampaignsController
    : LocationScopedRepositoryControllerBase<DonationCampaign, int, IDonationCampaignDataService>
{
    private readonly IConfigurationDataService _configurationDataService;

    public DonationCampaignsController(IDonationCampaignDataService dataService,
        ILocationDataService locationDataService,
        IConfigurationDataService configurationDataService) : base(dataService, locationDataService, x => x.DonationCampaignId)
    {
        _configurationDataService = configurationDataService;
    }

    /// <summary>
    ///  Gets the donation campaigns visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("GetDonationCampaigns")]
    [SwaggerResponse(statusCode: 200, type: typeof(PageResponse<DonationCampaign>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(PageResponse<DonationCampaign>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PageResponse<DonationCampaign>>> GetAllAsync(
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

        ServiceResponse<List<DonationCampaign>> result = await DataService.GetAllForLocationAsync(DataService.GetUserLocationId());
        if (!result.Success || result.Data == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
        }

        return await GetPageCoreAsync(pageNumber, itemsPerPage, _configurationDataService, result.Data);
    }


    /// <summary>
    /// Gets a donation campaign by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("GetDonationCampaign")]
    [SwaggerResponse(statusCode: 200, type: typeof(DonationCampaign), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The donation campaign is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Donation campaign not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(DonationCampaign), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DonationCampaign>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a donation campaign in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateDonationCampaign")]
    [SwaggerResponse(statusCode: 201, type: typeof(DonationCampaign), description: "Donation campaign created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid donation campaign")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(DonationCampaign), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DonationCampaign>> CreateAsync([FromBody] DonationCampaign campaign) =>
        await CreateScopedCoreAsync(campaign);

    /// <summary>
    /// Updates a donation campaign in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateDonationCampaign")]
    [SwaggerResponse(statusCode: 200, type: typeof(DonationCampaign), description: "Donation campaign updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid donation campaign")]
    [SwaggerResponse(statusCode: 403, description: "The donation campaign is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Donation campaign not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(DonationCampaign), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DonationCampaign>> UpdateAsync(int id, [FromBody] DonationCampaign campaign) =>
        await UpdateScopedCoreAsync(id, campaign);

    /// <summary>
    /// Deletes a donation campaign in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpDelete("{id:int}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteDonationCampaign")]
    [SwaggerResponse(statusCode: 204, description: "Donation campaign deleted")]
    [SwaggerResponse(statusCode: 403, description: "The donation campaign is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "Donation campaign not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
