using BedBrigade.Client.Controllers;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BedBrigade.Tests.Integration;

[TestFixture]
public class LocationsControllerTests
{
    private const int InternalServerErrorStatusCode = 500;

    [Test]
    public void LocationsController_ShouldUseRepositoryControllerPattern()
    {
        Assert.That(typeof(LocationsController).BaseType,
            Is.EqualTo(typeof(RepositoryControllerBase<Location, int, ILocationDataService>)));
    }

    [TestCase(nameof(LocationsController.GetAllAsync), RoleNames.CanViewLocations)]
    [TestCase(nameof(LocationsController.GetByIdAsync), RoleNames.CanViewLocations)]
    [TestCase(nameof(LocationsController.UpdateAsync), $"{RoleNames.NationalAdmin}, {RoleNames.LocationAdmin}")]
    public void ControllerAction_ShouldUseExpectedRole(string actionName, string expectedRoles)
    {
        var method = typeof(LocationsController).GetMethods()
            .Single(method => method.Name == actionName);

        AuthorizeAttribute authorizeAttribute = method.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.That(authorizeAttribute.Roles, Is.EqualTo(expectedRoles));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnOk_WhenLocationServiceSucceeds()
    {
        List<Location> locations =
        [
            new Location
            {
                LocationId = 10,
                Name = "Columbus",
                Route = "/columbus",
                BuildPostalCode = "43085",
                IsActive = true,
                TimeZoneId = "Eastern Standard Time"
            }
        ];

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new ServiceResponse<List<Location>>("Found 1 active locations", true, locations));
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();

        LocationsController controller = new(locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<Location>> result = await controller.GetAllAsync(1, 10);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<Location> payload = okResult.Value as PageResponse<Location>
            ?? throw new AssertionException("Expected a location page payload.");

        Assert.Multiple(() =>
        {
            Assert.That(okResult.StatusCode ?? StatusCodes.Status200OK, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(payload.Items, Has.Count.EqualTo(1));
            Assert.That(payload.Items[0].LocationId, Is.EqualTo(10));
            Assert.That(payload.Items[0].Name, Is.EqualTo("Columbus"));
            Assert.That(payload.Items[0].Route, Is.EqualTo("/columbus"));
        });
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnInternalServerError_WhenLocationServiceFails()
    {
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new ServiceResponse<List<Location>>("Unable to load locations."));
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();

        LocationsController controller = new(locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<Location>> result = await controller.GetAllAsync(1, 10);

        ApiError error = AssertApiErrorResponse(result.Result, InternalServerErrorStatusCode);
        Assert.That(error.Message, Is.EqualTo("Unable to load locations."));
    }

    [Test]
    public void GetAllAsync_ShouldThrow_WhenLocationServiceThrows()
    {
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetAllAsync())
            .ThrowsAsync(new InvalidOperationException("Boom"));
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();

        LocationsController controller = new(locationDataService.Object, configurationDataService.Object);

        // Note: The current LocationsController.GetAllAsync doesn't have exception handling,
        // so exceptions are not caught and propagate to the caller
        Assert.ThrowsAsync<InvalidOperationException>(async () => await controller.GetAllAsync(1, 10));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnOk_WhenLocationExists()
    {
        Location location = CreateLocation(10);
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(new ServiceResponse<Location>("Location found", true, location));
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();

        LocationsController controller = new(locationDataService.Object, configurationDataService.Object);

        ActionResult<Location> result = await controller.GetByIdAsync(10);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        Assert.That(okResult.Value, Is.SameAs(location));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync(new ServiceResponse<Location>("Not Found"));
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();

        LocationsController controller = new(locationDataService.Object, configurationDataService.Object);

        ActionResult<Location> result = await controller.GetByIdAsync(99);

        ApiError error = AssertApiErrorResponse(result.Result, StatusCodes.Status404NotFound);
        Assert.That(error.Message, Is.EqualTo("Not Found"));
    }



    [Test]
    public async Task UpdateAsync_ShouldReturnBadRequest_WhenRouteIdDoesNotMatchLocationId()
    {
        Location location = CreateLocation(10);
        Mock<ILocationDataService> locationDataService = new();
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();
        LocationsController controller = new(locationDataService.Object, configurationDataService.Object);

        ActionResult<Location> result = await controller.UpdateAsync(11, location);

        ApiError error = AssertApiErrorResponse(result.Result, StatusCodes.Status400BadRequest);
        Assert.That(error.Message, Is.EqualTo("The route id must match the location id."));
        locationDataService.Verify(x => x.UpdateAsync(It.IsAny<Location>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnOk_WhenLocationIsUpdated()
    {
        Location location = CreateLocation(10);
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.UpdateAsync(location))
            .ReturnsAsync(new ServiceResponse<Location>("Location updated", true, location));
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();

        LocationsController controller = new(locationDataService.Object, configurationDataService.Object);

        ActionResult<Location> result = await controller.UpdateAsync(10, location);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        Assert.That(okResult.Value, Is.SameAs(location));
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnForbidden_WhenLocationAdminUpdatesAnotherLocation()
    {
        Location location = CreateLocation(10);
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetUserRole()).Returns(RoleNames.LocationAdmin);
        locationDataService.Setup(x => x.GetUserLocationId()).Returns(20);
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();

        LocationsController controller = new(locationDataService.Object, configurationDataService.Object);

        ActionResult<Location> result = await controller.UpdateAsync(10, location);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
        locationDataService.Verify(x => x.UpdateAsync(It.IsAny<Location>()), Times.Never);
    }





    private static Location CreateLocation(int id)
    {
        return new Location
        {
            LocationId = id,
            Name = "Columbus",
            Route = "/columbus",
            BuildPostalCode = "43085",
            IsActive = true,
            TimeZoneId = "Eastern Standard Time"
        };
    }

    private static Mock<IConfigurationDataService> CreatePagingConfigurationDataService()
    {
        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        return configurationDataService;
    }

    private static ApiError AssertApiErrorResponse(IActionResult? result, int statusCode)
    {
        ObjectResult objectResult = result as ObjectResult
            ?? throw new AssertionException("Expected an error response.");

        Assert.That(objectResult.StatusCode, Is.EqualTo(statusCode));

        return objectResult.Value as ApiError
            ?? throw new AssertionException("Expected an ApiError payload.");
    }
}
