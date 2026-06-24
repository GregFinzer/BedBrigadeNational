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
/// Provides API endpoints for managing donation campaigns within the authenticated user's location scope.
/// </summary>
public class DonationCampaignsController
    : LocationScopedRepositoryControllerBase<DonationCampaign, int, IDonationCampaignDataService>
{
    public DonationCampaignsController(IDonationCampaignDataService dataService,
        ILocationDataService locationDataService) : base(dataService, locationDataService, x => x.DonationCampaignId)
    {
    }

    /// <summary>
    /// Gets the donation campaigns visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpGet]
    [SwaggerOperation("GetDonationCampaigns")]
    public async Task<ActionResult<List<DonationCampaign>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a donation campaign by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetDonationCampaign")]
    public async Task<ActionResult<DonationCampaign>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a donation campaign in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpPost]
    [SwaggerOperation("CreateDonationCampaign")]
    public async Task<ActionResult<DonationCampaign>> CreateAsync([FromBody] DonationCampaign campaign) =>
        await CreateScopedCoreAsync(campaign);

    /// <summary>
    /// Updates a donation campaign in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateDonationCampaign")]
    public async Task<ActionResult<DonationCampaign>> UpdateAsync(int id, [FromBody] DonationCampaign campaign) =>
        await UpdateScopedCoreAsync(id, campaign);

    /// <summary>
    /// Deletes a donation campaign in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonationCampaigns)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteDonationCampaign")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
