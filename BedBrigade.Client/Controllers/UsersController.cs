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
/// Provides API endpoints for managing users within the authenticated user's location scope.
/// </summary>
public class UsersController : LocationScopedRepositoryControllerBase<User, string, IUserDataService>
{
    public UsersController(IUserDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.UserName)
    {
    }

    /// <summary>
    /// Gets the users visible to the authenticated user.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewUsers)]
    [HttpGet]
    [SwaggerOperation("GetUsers")]
    public async Task<ActionResult<List<User>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewUsers)]
    [HttpGet("{id}")]
    [SwaggerOperation("GetUser")]
    public async Task<ActionResult<User>> GetByIdAsync(string id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a user in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageUsers)]
    [HttpPost]
    [SwaggerOperation("CreateUser")]
    public async Task<ActionResult<User>> CreateAsync([FromBody] User user) => await CreateScopedCoreAsync(user);

    /// <summary>
    /// Updates a user in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageUsers)]
    [HttpPut("{id}")]
    [SwaggerOperation("UpdateUser")]
    public async Task<ActionResult<User>> UpdateAsync(string id, [FromBody] User user) =>
        await UpdateScopedCoreAsync(id, user);

    /// <summary>
    /// Deletes a user in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageUsers)]
    [HttpDelete("{id}")]
    [SwaggerOperation("DeleteUser")]
    public async Task<IActionResult> DeleteAsync(string id) => await DeleteScopedCoreAsync(id);
}
