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
    [Produces("application/json")]
    [SwaggerOperation("GetUsers")]
    [SwaggerResponse(statusCode: 200, type: typeof(List<User>), description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(List<User>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<User>>> GetAllAsync() => await GetScopedAllCoreAsync();

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    [Authorize(Roles = RoleNames.CanViewUsers)]
    [HttpGet("{id}")]
    [Produces("application/json")]
    [SwaggerOperation("GetUser")]
    [SwaggerResponse(statusCode: 200, type: typeof(User), description: "Successful operation")]
    [SwaggerResponse(statusCode: 403, description: "The user is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "User not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> GetByIdAsync(string id) => await GetScopedByIdCoreAsync(id);

    /// <summary>
    /// Creates a user in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageUsers)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("CreateUser")]
    [SwaggerResponse(statusCode: 201, type: typeof(User), description: "User created")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid user")]
    [SwaggerResponse(statusCode: 403, description: "The location is outside the user's location scope")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> CreateAsync([FromBody] User user) => await CreateScopedCoreAsync(user);

    /// <summary>
    /// Updates a user in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageUsers)]
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("UpdateUser")]
    [SwaggerResponse(statusCode: 200, type: typeof(User), description: "User updated")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Invalid user")]
    [SwaggerResponse(statusCode: 403, description: "The user is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "User not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> UpdateAsync(string id, [FromBody] User user) =>
        await UpdateScopedCoreAsync(id, user);

    /// <summary>
    /// Deletes a user in the authenticated user's location scope.
    /// </summary>
    [Authorize(Roles = RoleNames.CanManageUsers)]
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [SwaggerOperation("DeleteUser")]
    [SwaggerResponse(statusCode: 204, description: "User deleted")]
    [SwaggerResponse(statusCode: 403, description: "The user is outside the user's location scope")]
    [SwaggerResponse(statusCode: 404, type: typeof(ApiError), description: "User not found")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(string id) => await DeleteScopedCoreAsync(id);
}
