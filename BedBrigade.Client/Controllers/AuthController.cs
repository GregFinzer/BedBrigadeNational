using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string LockedMessagePrefix = "Your account is locked for";
    private readonly IAuthDataService _authDataService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(IAuthDataService authDataService, IJwtTokenService jwtTokenService)
    {
        _authDataService = authDataService ?? throw new ArgumentNullException(nameof(authDataService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
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


