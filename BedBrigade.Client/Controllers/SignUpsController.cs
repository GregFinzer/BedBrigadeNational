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
/// Provides API endpoints for managing volunteer sign-ups within the authenticated user's location scope.
/// </summary>
public class SignUpsController : LocationScopedRepositoryControllerBase<SignUp, int, ISignUpDataService>
{
    public SignUpsController(ISignUpDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.SignUpId)
    {
    }

    /// <summary>
    /// Gets the sign-ups visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewSignUps)]
    [HttpGet]
    [SwaggerOperation("GetSignUps")]
    public async Task<ActionResult<List<SignUp>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a sign-up by its identifier.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewSignUps)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetSignUp")]
    public async Task<ActionResult<SignUp>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a sign-up in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPost]
    [SwaggerOperation("CreateSignUp")]
    public async Task<ActionResult<SignUp>> CreateAsync([FromBody] SignUp signUp) =>
        await CreateScopedCoreAsync(signUp);

    /// <summary>
    /// Updates a sign-up in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateSignUp")]
    public async Task<ActionResult<SignUp>> UpdateAsync(int id, [FromBody] SignUp signUp) =>
        await UpdateScopedCoreAsync(id, signUp);

    /// <summary>
    /// Deletes a sign-up in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteSignUp")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
