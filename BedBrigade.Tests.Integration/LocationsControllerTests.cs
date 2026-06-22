using BedBrigade.Client.Controllers;
using BedBrigade.Common.Constants;
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
        locationDataService.Setup(x => x.GetActiveLocations())
            .ReturnsAsync(new ServiceResponse<List<Location>>("Found 1 active locations", true, locations));

        LocationsController controller = new(locationDataService.Object);

        ActionResult<List<Location>> result = await controller.GetAllAsync();

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        List<Location> payload = okResult.Value as List<Location>
            ?? throw new AssertionException("Expected a location list payload.");

        Assert.Multiple(() =>
        {
            Assert.That(okResult.StatusCode ?? StatusCodes.Status200OK, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(payload, Has.Count.EqualTo(1));
            Assert.That(payload[0].LocationId, Is.EqualTo(10));
            Assert.That(payload[0].Name, Is.EqualTo("Columbus"));
            Assert.That(payload[0].Route, Is.EqualTo("/columbus"));
        });
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnInternalServerError_WhenLocationServiceFails()
    {
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetActiveLocations())
            .ReturnsAsync(new ServiceResponse<List<Location>>("Unable to load locations."));

        LocationsController controller = new(locationDataService.Object);

        ActionResult<List<Location>> result = await controller.GetAllAsync();

        ApiError error = AssertApiErrorResponse(result.Result, InternalServerErrorStatusCode);
        Assert.That(error.Message, Is.EqualTo("Unable to load locations."));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnInternalServerError_WhenLocationServiceThrows()
    {
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetActiveLocations())
            .ThrowsAsync(new InvalidOperationException("Boom"));

        LocationsController controller = new(locationDataService.Object);

        ActionResult<List<Location>> result = await controller.GetAllAsync();

        ApiError error = AssertApiErrorResponse(result.Result, InternalServerErrorStatusCode);
        Assert.That(error.Message, Is.EqualTo("There was an error getting locations, try again later."));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnOk_WhenLocationExists()
    {
        Location location = CreateLocation(10);
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(new ServiceResponse<Location>("Location found", true, location));

        LocationsController controller = new(locationDataService.Object);

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

        LocationsController controller = new(locationDataService.Object);

        ActionResult<Location> result = await controller.GetByIdAsync(99);

        ApiError error = AssertApiErrorResponse(result.Result, StatusCodes.Status404NotFound);
        Assert.That(error.Message, Is.EqualTo("Not Found"));
    }

    [Test]
    public async Task CreateAsync_ShouldReturnCreatedAtAction_WhenLocationIsCreated()
    {
        Location location = CreateLocation(10);
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.CreateAsync(location))
            .ReturnsAsync(new ServiceResponse<Location>("Location created", true, location));

        LocationsController controller = new(locationDataService.Object);

        ActionResult<Location> result = await controller.CreateAsync(location);

        CreatedAtActionResult createdResult = result.Result as CreatedAtActionResult
            ?? throw new AssertionException("Expected a created response.");
        Assert.Multiple(() =>
        {
            Assert.That(createdResult.ActionName, Is.EqualTo(nameof(LocationsController.GetByIdAsync)));
            Assert.That(createdResult.RouteValues?["id"], Is.EqualTo(10));
            Assert.That(createdResult.Value, Is.SameAs(location));
        });
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnBadRequest_WhenRouteIdDoesNotMatchLocationId()
    {
        Location location = CreateLocation(10);
        Mock<ILocationDataService> locationDataService = new();
        LocationsController controller = new(locationDataService.Object);

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

        LocationsController controller = new(locationDataService.Object);

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

        LocationsController controller = new(locationDataService.Object);

        ActionResult<Location> result = await controller.UpdateAsync(10, location);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
        locationDataService.Verify(x => x.UpdateAsync(It.IsAny<Location>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenLocationIsDeleted()
    {
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.DeleteAsync(10))
            .ReturnsAsync(new ServiceResponse<bool>("Location deleted", true, true));

        LocationsController controller = new(locationDataService.Object);

        IActionResult result = await controller.DeleteAsync(10);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenLocationCannotBeDeleted()
    {
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.DeleteAsync(99))
            .ReturnsAsync(new ServiceResponse<bool>("Location not found"));

        LocationsController controller = new(locationDataService.Object);

        IActionResult result = await controller.DeleteAsync(99);

        ApiError error = AssertApiErrorResponse(result, StatusCodes.Status404NotFound);
        Assert.That(error.Message, Is.EqualTo("Location not found"));
    }

    [Test]
    public void LocationsController_ShouldRequireCanViewLocationsRole()
    {
        AuthorizeAttribute? authorizeAttribute = typeof(LocationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.That(authorizeAttribute, Is.Not.Null);
        Assert.That(authorizeAttribute!.Roles, Is.EqualTo(RoleNames.CanViewLocations));
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

    private static ApiError AssertApiErrorResponse(IActionResult? result, int statusCode)
    {
        ObjectResult objectResult = result as ObjectResult
            ?? throw new AssertionException("Expected an error response.");

        Assert.That(objectResult.StatusCode, Is.EqualTo(statusCode));

        return objectResult.Value as ApiError
            ?? throw new AssertionException("Expected an ApiError payload.");
    }
}
