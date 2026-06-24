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
/// Provides API endpoints for managing donations within the authenticated user's location scope.
/// </summary>
public class DonationsController : LocationScopedRepositoryControllerBase<Donation, int, IDonationDataService>
{
    public DonationsController(IDonationDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.DonationId)
    {
    }

    /// <summary>
    /// Gets the donations visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpGet]
    [SwaggerOperation("GetDonations")]
    public async Task<ActionResult<List<Donation>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a donation by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetDonation")]
    public async Task<ActionResult<Donation>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a donation in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpPost]
    [SwaggerOperation("CreateDonation")]
    public async Task<ActionResult<Donation>> CreateAsync([FromBody] Donation donation) =>
        await CreateScopedCoreAsync(donation);

    /// <summary>
    /// Updates a donation in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateDonation")]
    public async Task<ActionResult<Donation>> UpdateAsync(int id, [FromBody] Donation donation) =>
        await UpdateScopedCoreAsync(id, donation);

    /// <summary>
    /// Deletes a donation in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageDonations)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteDonation")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
