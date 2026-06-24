using System.Security.Claims;
using BedBrigade.Common.Constants;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BedBrigade.Tests;

[TestFixture]
public class AuthServiceTests
{
    private const string ControllerEmail = "controller@example.org";
    private const string ControllerUserName = "Controller User";
    private const string ControllerRoute = "controller-route";
    private const string ControllerTimeZoneId = "Central Standard Time";
    private const string ControllerPhone = "(614) 555-1212";
    private const int ControllerLocationId = 20;

    [Test]
    public void UserProperties_ShouldUseAuthenticatedHttpContextUser_WhenRunningInController()
    {
        AuthService authService = CreateAuthService(CreateAuthenticatedUser(
            ControllerLocationId,
            ControllerEmail,
            ControllerUserName,
            RoleNames.NationalAdmin,
            ControllerRoute,
            ControllerTimeZoneId,
            ControllerPhone));
        authService.CurrentUser = CreateAuthenticatedUser(10);

        Assert.Multiple(() =>
        {
            Assert.That(authService.Email, Is.EqualTo(ControllerEmail));
            Assert.That(authService.UserName, Is.EqualTo(ControllerUserName));
            Assert.That(authService.UserRole, Is.EqualTo(RoleNames.NationalAdmin));
            Assert.That(authService.UserRoute, Is.EqualTo(ControllerRoute));
            Assert.That(authService.LocationId, Is.EqualTo(ControllerLocationId));
            Assert.That(authService.TimeZoneId, Is.EqualTo(ControllerTimeZoneId));
            Assert.That(authService.Phone, Is.EqualTo(ControllerPhone));
            Assert.That(authService.IsLoggedIn, Is.True);
            Assert.That(authService.IsNationalAdmin, Is.True);
            Assert.That(authService.UserHasRole(RoleNames.NationalAdmin), Is.True);
        });
    }

    [Test]
    public void UserProperties_ShouldUseCurrentUser_WhenHttpContextUserIsNotAuthenticated()
    {
        const int currentUserLocationId = 10;
        AuthService authService = CreateAuthService(new ClaimsPrincipal(new ClaimsIdentity()));
        authService.CurrentUser = CreateAuthenticatedUser(
            currentUserLocationId,
            "blazor@example.org",
            "Blazor User",
            RoleNames.LocationAdmin,
            "blazor-route",
            Defaults.DefaultTimeZoneId,
            "(740) 555-1212");

        Assert.Multiple(() =>
        {
            Assert.That(authService.Email, Is.EqualTo("blazor@example.org"));
            Assert.That(authService.UserName, Is.EqualTo("Blazor User"));
            Assert.That(authService.UserRole, Is.EqualTo(RoleNames.LocationAdmin));
            Assert.That(authService.UserRoute, Is.EqualTo("blazor-route"));
            Assert.That(authService.LocationId, Is.EqualTo(currentUserLocationId));
            Assert.That(authService.TimeZoneId, Is.EqualTo(Defaults.DefaultTimeZoneId));
            Assert.That(authService.Phone, Is.EqualTo("(740) 555-1212"));
            Assert.That(authService.IsLoggedIn, Is.True);
            Assert.That(authService.IsNationalAdmin, Is.False);
            Assert.That(authService.UserHasRole(RoleNames.LocationAdmin), Is.True);
        });
    }

    [Test]
    public void UserProperties_ShouldReturnDefaults_WhenNoUserIsAuthenticated()
    {
        AuthService authService = CreateAuthService(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.Multiple(() =>
        {
            Assert.That(authService.Email, Is.EqualTo(Defaults.DefaultUserNameAndEmail));
            Assert.That(authService.UserName, Is.EqualTo(Defaults.DefaultUserNameAndEmail));
            Assert.That(authService.UserRole, Is.Null);
            Assert.That(authService.UserRoute, Is.Null);
            Assert.That(authService.LocationId, Is.Zero);
            Assert.That(authService.TimeZoneId, Is.EqualTo(Defaults.DefaultTimeZoneId));
            Assert.That(authService.IsLoggedIn, Is.False);
            Assert.That(authService.IsNationalAdmin, Is.False);
            Assert.That(authService.UserHasRole(RoleNames.NationalAdmin), Is.False);
        });
    }

    private static AuthService CreateAuthService(ClaimsPrincipal httpContextUser)
    {
        DefaultHttpContext httpContext = new()
        {
            User = httpContextUser
        };
        HttpContextAccessor httpContextAccessor = new()
        {
            HttpContext = httpContext
        };

        return new AuthService(
            new Mock<ICustomSessionService>().Object,
            new Mock<IJwtTokenService>().Object,
            httpContextAccessor);
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(
        int locationId,
        string email = "user@example.org",
        string userName = "User",
        string role = RoleNames.LocationAdmin,
        string userRoute = "route",
        string timeZoneId = Defaults.DefaultTimeZoneId,
        string phone = "(614) 555-0000")
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.NameIdentifier, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim("UserRoute", userRoute),
            new Claim("LocationId", locationId.ToString()),
            new Claim("TimeZoneId", timeZoneId),
            new Claim("Phone", phone)
        ], "jwt"));
    }
}
