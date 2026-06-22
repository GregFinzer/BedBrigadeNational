using BedBrigade.Client.Controllers;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace BedBrigade.Tests.Integration;

[TestFixture]
public class AuthControllerTests
{
    private const string Email = "integration@example.com";
    private const string UserName = "integration-user";
    private const string Password = "CorrectPassword123!";
    private const string WrongPassword = "WrongPassword123!";
    private const string JwtToken = "jwt-token";
    private const string MissingCredentialsMessage = "Please enter your email and password.";
    private const string UserNotFoundMessage = "User not found.";
    private const string WrongPasswordMessage = "Wrong password.";
    private const string UnexpectedMessage = "Unexpected failure.";
    private const string LockedMessagePrefix = "Your account is locked for";
    private const int BadRequestStatusCode = 400;
    private const int UnauthorizedStatusCode = 401;
    private const int NotFoundStatusCode = 404;
    private const int LockedStatusCode = 423;
    private const int InternalServerErrorStatusCode = 500;

    [Test]
    public async Task Login_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        Mock<IAuthDataService> authDataService = new();
        AuthController controller = CreateController(authDataService.Object, new Mock<IJwtTokenService>().Object, new Mock<IAuthService>().Object);
        controller.ModelState.AddModelError(nameof(UserLogin.Email), "Email Address is required");

        ActionResult<AuthTokenResponse> result = await controller.Login(new UserLogin());

        ApiError error = AssertApiErrorResponse(result, BadRequestStatusCode);
        Assert.That(error.Message, Is.EqualTo(MissingCredentialsMessage));
    }

    [Test]
    public async Task Login_ShouldReturnBadRequest_WhenAuthDataServiceReturnsMissingCredentialsMessage()
    {
        Mock<IAuthDataService> authDataService = new();
        authDataService.Setup(x => x.Login(Email, Password))
            .ReturnsAsync(new ServiceResponse<ClaimsPrincipal>(MissingCredentialsMessage));

        AuthController controller = CreateController(authDataService.Object, new Mock<IJwtTokenService>().Object, new Mock<IAuthService>().Object);

        ActionResult<AuthTokenResponse> result = await controller.Login(new UserLogin { Email = Email, Password = Password });

        ApiError error = AssertApiErrorResponse(result, BadRequestStatusCode);
        Assert.That(error.Message, Is.EqualTo(MissingCredentialsMessage));
    }

    [Test]
    public async Task Login_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var (controller, _, jwtTokenService) = CreateRealLoginController();

        ActionResult<AuthTokenResponse> result = await controller.Login(new UserLogin { Email = Email, Password = Password });

        ApiError error = AssertApiErrorResponse(result, NotFoundStatusCode);
        Assert.That(error.Message, Is.EqualTo(UserNotFoundMessage));
        jwtTokenService.Verify(x => x.GenerateToken(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Test]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        var (controller, authDataService, jwtTokenService) = CreateRealLoginController();
        await SeedUserAsync(authDataService);

        ActionResult<AuthTokenResponse> result = await controller.Login(new UserLogin { Email = Email, Password = WrongPassword });

        ApiError error = AssertApiErrorResponse(result, UnauthorizedStatusCode);
        Assert.That(error.Message, Is.EqualTo(WrongPasswordMessage));
        jwtTokenService.Verify(x => x.GenerateToken(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Test]
    public async Task Login_ShouldReturnLocked_WhenUserIsLockedOut()
    {
        var (controller, authDataService, jwtTokenService) = CreateRealLoginController();
        await SeedUserAsync(authDataService);

        await authDataService.Login(Email, WrongPassword);
        await authDataService.Login(Email, WrongPassword);
        await authDataService.Login(Email, WrongPassword);

        ActionResult<AuthTokenResponse> result = await controller.Login(new UserLogin { Email = Email, Password = Password });

        ApiError error = AssertApiErrorResponse(result, LockedStatusCode);
        Assert.That(error.Message, Does.StartWith(LockedMessagePrefix));
        jwtTokenService.Verify(x => x.GenerateToken(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Test]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        var (controller, authDataService, jwtTokenService) = CreateRealLoginController();
        await SeedUserAsync(authDataService);
        jwtTokenService.Setup(x => x.GenerateToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(new AuthTokenResponse { Token = JwtToken, TokenType = "Bearer" });

        ActionResult<AuthTokenResponse> result = await controller.Login(new UserLogin { Email = Email, Password = Password });

        AuthTokenResponse tokenResponse = AssertOkTokenResponse(result);
        Assert.Multiple(() =>
        {
            Assert.That(tokenResponse.Token, Is.EqualTo(JwtToken));
            Assert.That(tokenResponse.TokenType, Is.EqualTo("Bearer"));
        });
        jwtTokenService.Verify(x => x.GenerateToken(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Test]
    public async Task Login_ShouldReturnInternalServerError_WhenAuthDataServiceReturnsUnexpectedMessage()
    {
        Mock<IAuthDataService> authDataService = new();
        authDataService.Setup(x => x.Login(Email, Password))
            .ReturnsAsync(new ServiceResponse<ClaimsPrincipal>(UnexpectedMessage));

        AuthController controller = CreateController(authDataService.Object, new Mock<IJwtTokenService>().Object, new Mock<IAuthService>().Object);

        ActionResult<AuthTokenResponse> result = await controller.Login(new UserLogin { Email = Email, Password = Password });

        ApiError error = AssertApiErrorResponse(result, InternalServerErrorStatusCode);
        Assert.That(error.Message, Is.EqualTo(UnexpectedMessage));
    }

    [Test]
    public async Task Login_ShouldReturnInternalServerError_WhenTokenGenerationThrows()
    {
        var (controller, authDataService, jwtTokenService) = CreateRealLoginController();
        await SeedUserAsync(authDataService);
        jwtTokenService.Setup(x => x.GenerateToken(It.IsAny<ClaimsPrincipal>()))
            .Throws(new InvalidOperationException("Token generation failed."));

        ActionResult<AuthTokenResponse> result = await controller.Login(new UserLogin { Email = Email, Password = Password });

        ApiError error = AssertApiErrorResponse(result, InternalServerErrorStatusCode);
        Assert.That(error.Message, Is.EqualTo("There was an error logging in, try again later."));
    }

    [Test]
    public async Task Logout_ShouldReturnOk_WhenLogoutSucceeds()
    {
        Mock<IAuthService> authService = new();
        authService.Setup(x => x.LogoutAsync(false)).Returns(Task.CompletedTask);
        AuthController controller = CreateController(new Mock<IAuthDataService>().Object, new Mock<IJwtTokenService>().Object, authService.Object);

        IActionResult result = await controller.Logout();

        Assert.That(result, Is.TypeOf<OkResult>());
        authService.Verify(x => x.LogoutAsync(false), Times.Once);
    }

    [Test]
    public async Task Logout_ShouldReturnInternalServerError_WhenLogoutThrows()
    {
        Mock<IAuthService> authService = new();
        authService.Setup(x => x.LogoutAsync(false)).ThrowsAsync(new InvalidOperationException("Logout failed."));
        AuthController controller = CreateController(new Mock<IAuthDataService>().Object, new Mock<IJwtTokenService>().Object, authService.Object);

        IActionResult result = await controller.Logout();

        ApiError error = AssertApiErrorResponse(result, InternalServerErrorStatusCode);
        Assert.That(error.Message, Is.EqualTo("There was an error logging out, try again later."));
        authService.Verify(x => x.LogoutAsync(false), Times.Once);
    }

    [Test]
    public void Me_ShouldReturnCurrentUserClaims_WhenUserIsAuthenticated()
    {
        AuthController controller = CreateController(new Mock<IAuthDataService>().Object, new Mock<IJwtTokenService>().Object, new Mock<IAuthService>().Object);
        controller.ControllerContext = CreateControllerContext(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, UserName),
            new Claim(ClaimTypes.Name, Email),
            new Claim(ClaimTypes.Role, RoleNames.NationalAdmin),
            new Claim("LocationId", Defaults.NationalLocationId.ToString()),
            new Claim("UserRoute", "national"),
            new Claim("TimeZoneId", Defaults.DefaultTimeZoneId),
            new Claim("Phone", "(614) 555-1212")
        ], "jwt")));

        ActionResult<AuthenticatedUserResponse> result = controller.Me();

        AuthenticatedUserResponse response = AssertOkResponse<AuthenticatedUserResponse>(result);
        Assert.Multiple(() =>
        {
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.UserName, Is.EqualTo(UserName));
            Assert.That(response.Email, Is.EqualTo(Email));
            Assert.That(response.Role, Is.EqualTo(RoleNames.NationalAdmin));
            Assert.That(response.LocationId, Is.EqualTo(Defaults.NationalLocationId));
            Assert.That(response.UserRoute, Is.EqualTo("national"));
            Assert.That(response.TimeZoneId, Is.EqualTo(Defaults.DefaultTimeZoneId));
            Assert.That(response.Phone, Is.EqualTo("(614) 555-1212"));
        });
    }

    [Test]
    public void Me_ShouldReturnDefaultValues_WhenCalledWithoutAuthenticatedUser()
    {
        AuthController controller = CreateController(new Mock<IAuthDataService>().Object, new Mock<IJwtTokenService>().Object, new Mock<IAuthService>().Object);
        controller.ControllerContext = CreateControllerContext(new ClaimsPrincipal(new ClaimsIdentity()));

        ActionResult<AuthenticatedUserResponse> result = controller.Me();

        AuthenticatedUserResponse response = AssertOkResponse<AuthenticatedUserResponse>(result);
        Assert.Multiple(() =>
        {
            Assert.That(response.IsAuthenticated, Is.False);
            Assert.That(response.UserName, Is.Empty);
            Assert.That(response.Email, Is.Empty);
            Assert.That(response.Role, Is.Empty);
            Assert.That(response.LocationId, Is.EqualTo(0));
        });
    }

    private static AuthController CreateController(IAuthDataService authDataService, IJwtTokenService jwtTokenService, IAuthService authService)
    {
        return new AuthController(authDataService, jwtTokenService, authService);
    }

    private static ControllerContext CreateControllerContext(ClaimsPrincipal user)
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
    }

    private static AuthDataService CreateAuthDataService()
    {
        DbContextOptions<DataContext> options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        IDbContextFactory<DataContext> contextFactory = new TestDataContextFactory(options);
        Mock<ILocationDataService> locationDataService = new();
        Mock<IConfigurationDataService> configurationDataService = new();
        ICachingService cachingService = new CachingService { IsCachingEnabled = false };

        locationDataService.Setup(x => x.GetByIdAsync(It.IsAny<object>()))
            .ReturnsAsync(new ServiceResponse<Location>("Location found", true, new Location
            {
                LocationId = Defaults.NationalLocationId,
                Name = "National",
                Route = "national",
                TimeZoneId = Defaults.DefaultTimeZoneId
            }));

        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxLoginAttempts))
            .ReturnsAsync(3);
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.LockoutMinutes))
            .ReturnsAsync(30);

        return new AuthDataService(contextFactory, locationDataService.Object, cachingService, configurationDataService.Object);
    }

    private static async Task SeedUserAsync(AuthDataService authDataService)
    {
        User user = new()
        {
            UserName = UserName,
            Email = Email,
            FirstName = "Integration",
            LastName = "Tester",
            Phone = "6145551212",
            Role = RoleNames.NationalAdmin,
            FkRole = 1,
            LocationId = Defaults.NationalLocationId
        };

        ServiceResponse<bool> registerResult = await authDataService.Register(user, Password);
        Assert.That(registerResult.Success, Is.True, registerResult.Message);
    }

    private static AuthTokenResponse AssertOkTokenResponse(ActionResult<AuthTokenResponse> result)
    {
        return AssertOkResponse<AuthTokenResponse>(result);
    }

    private static T AssertOkResponse<T>(ActionResult<T> result) where T : class
    {
        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");

        return okResult.Value as T
            ?? throw new AssertionException($"Expected a {typeof(T).Name} payload.");
    }

    private static ApiError AssertApiErrorResponse(ActionResult<AuthTokenResponse> result, int statusCode)
    {
        return AssertApiErrorResponse((IActionResult?)result.Result, statusCode);
    }

    private static ApiError AssertApiErrorResponse(IActionResult? result, int statusCode)
    {
        ObjectResult objectResult = result as ObjectResult
            ?? throw new AssertionException("Expected an error response.");

        Assert.That(objectResult.StatusCode, Is.EqualTo(statusCode));

        return objectResult.Value as ApiError
            ?? throw new AssertionException("Expected an ApiError payload.");
    }

    private static (AuthController controller, AuthDataService authDataService, Mock<IJwtTokenService> jwtTokenService) CreateRealLoginController()
    {
        AuthDataService authDataService = CreateAuthDataService();
        Mock<IJwtTokenService> jwtTokenService = new();

        return (CreateController(authDataService, jwtTokenService.Object, new Mock<IAuthService>().Object), authDataService, jwtTokenService);
    }
}



