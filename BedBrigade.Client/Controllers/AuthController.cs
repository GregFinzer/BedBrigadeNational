using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Provides authentication endpoints for login, logout, and current-user identity details.
/// </summary>
public class AuthController : ControllerBase
{
    private const string LockedMessagePrefix = "Your account is locked for";
    private readonly IAuthDataService _authDataService;
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(IAuthDataService authDataService, IJwtTokenService jwtTokenService, IAuthService authService)
    {
        _authDataService = authDataService ?? throw new ArgumentNullException(nameof(authDataService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Authenticates a user and returns a JWT bearer token when the credentials are valid.
    /// </summary>
    /// <param name="request">The login request containing the user's email address and password.</param>
    /// <returns>A successful response containing the JWT token payload, or an <see cref="ApiError"/> with an appropriate HTTP status code.</returns>
    /// <response code="200">Returns an <see cref="AuthTokenResponse"/> containing the JWT token and token type.</response>
    /// <response code="400">Returns an <see cref="ApiError"/> when the email or password is missing or invalid.</response>
    /// <response code="401">Returns an <see cref="ApiError"/> when the password is incorrect.</response>
    /// <response code="404">Returns an <see cref="ApiError"/> when the user does not exist.</response>
    /// <response code="423">Returns an <see cref="ApiError"/> when the account is locked out.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when an unexpected error occurs.</response>
    [HttpPost("login")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation("Login")]
    [SwaggerResponse(statusCode: 200, type: typeof(AuthTokenResponse), description: "Successful operation")]
    [SwaggerResponse(statusCode: 400, type: typeof(AuthTokenResponse), description: "Email is missing or invalid")]
    [SwaggerResponse(statusCode: 401, type: typeof(AuthTokenResponse), description: "The password is invalid")]
    [SwaggerResponse(statusCode: 404, type: typeof(AuthTokenResponse), description: "The user account does not exist")]
    [SwaggerResponse(statusCode: 423, type: typeof(AuthTokenResponse), description: "The account is locked")]
    [SwaggerResponse(statusCode: 500, type: typeof(AuthTokenResponse), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthTokenResponse>> Login([FromBody] UserLogin request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateApiError(GetValidationMessage(request)));
        }

        try
        {
            ServiceResponse<ClaimsPrincipal> loginResult = await _authDataService.Login(request.Email, request.Password);
            if (!loginResult.Success || loginResult.Data == null)
            {
                return CreateFailureResponse(loginResult.Message);
            }

            AuthTokenResponse tokenResponse = _jwtTokenService.GenerateToken(loginResult.Data);
            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(AuthController), nameof(Login));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error logging in, try again later."));
        }
    }

    /// <summary>
    /// Logs out the currently authenticated user.
    /// </summary>
    /// <returns>A successful response when logout completes, or an <see cref="ApiError"/> if an unexpected error occurs.</returns>
    /// <response code="200">Logout was successful.</response>
    /// <response code="500">Returns an <see cref="ApiError"/> when an unexpected error occurs.</response>
    [Authorize]
    [HttpPost("logout")]
    [Produces("application/json")]
    [SwaggerOperation("Logout")]
    [SwaggerResponse(statusCode: 200, description: "Successful operation")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _authService.LogoutAsync(false);
            return Ok();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", nameof(AuthController), nameof(Logout));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError("There was an error logging out, try again later."));
        }
    }

    /// <summary>
    /// Temporary diagnostic endpoint to verify JWT bearer authentication.
    /// </summary>
    /// <returns>The authenticated user's claims as seen by the API.</returns>
    [Authorize]
    [HttpGet("me")]
    [Produces("application/json")]
    [SwaggerOperation("GetCurrentUser")]
    [SwaggerResponse(statusCode: 200, type: typeof(AuthenticatedUserResponse), description: "Successful operation")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    public ActionResult<AuthenticatedUserResponse> Me()
    {
        return Ok(new AuthenticatedUserResponse
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            UserName = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            UserRoute = User.FindFirstValue("UserRoute") ?? string.Empty,
            TimeZoneId = User.FindFirstValue("TimeZoneId") ?? string.Empty,
            Phone = User.FindFirstValue("Phone") ?? string.Empty,
            LocationId = ParseLocationId(User.FindFirstValue("LocationId"))
        });
    }

    private ActionResult<AuthTokenResponse> CreateFailureResponse(string message)
    {
        ApiError response = CreateApiError(message);

        return message switch
        {
            "Please enter your email and password." => BadRequest(response),
            "User not found." => NotFound(response),
            "Wrong password." => Unauthorized(response),
            _ when message.StartsWith(LockedMessagePrefix, StringComparison.Ordinal) =>
                StatusCode(StatusCodes.Status423Locked, response),
            _ => StatusCode(StatusCodes.Status500InternalServerError, response)
        };
    }

    private static ApiError CreateApiError(string message)
    {
        return new ApiError { Message = message };
    }

    private static int ParseLocationId(string? locationIdValue)
    {
        return int.TryParse(locationIdValue, out int locationId)
            ? locationId
            : 0;
    }

    private string GetValidationMessage(UserLogin request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return "Please enter your email and password.";
        }

        string? firstValidationMessage = ModelState.Values
            .SelectMany(modelState => modelState.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

        if (!string.IsNullOrWhiteSpace(firstValidationMessage))
        {
            return firstValidationMessage;
        }

        return "The login request is invalid.";
    }
}

