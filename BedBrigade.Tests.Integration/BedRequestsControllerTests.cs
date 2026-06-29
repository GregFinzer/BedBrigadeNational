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
public class BedRequestsControllerTests
{
    [Test]
    public void BedRequestsController_ShouldUseLocationScopedRepositoryControllerPattern()
    {
        Assert.That(typeof(BedRequestsController).BaseType,
            Is.EqualTo(typeof(LocationScopedRepositoryControllerBase<BedRequest, int, IBedRequestDataService>)));
    }

    [TestCase(nameof(BedRequestsController.GetBedRequests), RoleNames.CanViewBedRequests)]
    [TestCase(nameof(BedRequestsController.GetByIdAsync), RoleNames.CanViewBedRequests)]
    [TestCase(nameof(BedRequestsController.CreateAsync), RoleNames.CanManageBedRequests)]
    [TestCase(nameof(BedRequestsController.UpdateAsync), RoleNames.CanManageBedRequests)]
    [TestCase(nameof(BedRequestsController.DeleteAsync), RoleNames.CanManageBedRequests)]
    public void ControllerAction_ShouldUseExpectedRole(string actionName, string expectedRoles)
    {
        var method = typeof(BedRequestsController).GetMethods()
            .Single(method => method.Name == actionName);

        AuthorizeAttribute authorizeAttribute = method.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.That(authorizeAttribute.Roles, Is.EqualTo(expectedRoles));
    }

    [Test]
    public async Task GetBedRequests_ShouldReturnRequestsForUserLocation()
    {
        Location userLocation = CreateLocation(10);
        List<BedRequest> bedRequests =
        [
            new BedRequest { BedRequestId = 100, LocationId = userLocation.LocationId }
        ];

        Mock<IBedRequestDataService> bedRequestDataService = new();
        bedRequestDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        bedRequestDataService.Setup(x => x.LoadBedRequests(userLocation, null))
            .ReturnsAsync(new ServiceResponse<List<BedRequest>>("Found bed requests", true, bedRequests));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<BedRequest>> result = await controller.GetBedRequests(1, 1000);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<BedRequest> payload = okResult.Value as PageResponse<BedRequest>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.Multiple(() =>
        {
            Assert.That(payload.PageNumber, Is.EqualTo(1));
            Assert.That(payload.MaxPage, Is.EqualTo(1));
            Assert.That(payload.NumberOfItems, Is.EqualTo(1));
            Assert.That(payload.ItemsPerPage, Is.EqualTo(1000));
            Assert.That(payload.Items, Is.EqualTo(bedRequests));
        });
        locationDataService.Verify(x => x.GetLocationsByMetroAreaId(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetBedRequests_ShouldReturnRequestsForUserMetroArea()
    {
        Location userLocation = CreateLocation(10, 5);
        List<Location> metroLocations =
        [
            userLocation,
            CreateLocation(20, 5)
        ];
        List<BedRequest> bedRequests =
        [
            new BedRequest { BedRequestId = 100, LocationId = 10 },
            new BedRequest { BedRequestId = 200, LocationId = 20 }
        ];

        Mock<IBedRequestDataService> bedRequestDataService = new();
        bedRequestDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        bedRequestDataService.Setup(x => x.LoadBedRequests(userLocation, metroLocations))
            .ReturnsAsync(new ServiceResponse<List<BedRequest>>("Found bed requests", true, bedRequests));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));
        locationDataService.Setup(x => x.GetLocationsByMetroAreaId(5))
            .ReturnsAsync(new ServiceResponse<List<Location>>("Found metro locations", true, metroLocations));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<BedRequest>> result = await controller.GetBedRequests(1, 1000);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<BedRequest> payload = okResult.Value as PageResponse<BedRequest>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.That(payload.Items, Has.Count.EqualTo(2));
        bedRequestDataService.Verify(x => x.LoadBedRequests(userLocation, metroLocations), Times.Once);
    }

    [Test]
    public async Task GetBedRequests_ShouldReturnRequestedPage()
    {
        Location userLocation = CreateLocation(10);
        List<BedRequest> bedRequests =
        [
            CreateBedRequest(100, userLocation.LocationId),
            CreateBedRequest(200, userLocation.LocationId),
            CreateBedRequest(300, userLocation.LocationId)
        ];

        Mock<IBedRequestDataService> bedRequestDataService = new();
        bedRequestDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        bedRequestDataService.Setup(x => x.LoadBedRequests(userLocation, null))
            .ReturnsAsync(new ServiceResponse<List<BedRequest>>("Found bed requests", true, bedRequests));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<BedRequest>> result = await controller.GetBedRequests(2, 2);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<BedRequest> payload = okResult.Value as PageResponse<BedRequest>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.Multiple(() =>
        {
            Assert.That(payload.PageNumber, Is.EqualTo(2));
            Assert.That(payload.MaxPage, Is.EqualTo(2));
            Assert.That(payload.NumberOfItems, Is.EqualTo(3));
            Assert.That(payload.ItemsPerPage, Is.EqualTo(2));
            Assert.That(payload.Items.Select(x => x.BedRequestId), Is.EqualTo(new[] { 300 }));
        });
    }

    [Test]
    public async Task GetBedRequests_ShouldCapItemsPerPageAtOneThousand()
    {
        Location userLocation = CreateLocation(10);
        List<BedRequest> bedRequests =
        [
            CreateBedRequest(100, userLocation.LocationId),
            CreateBedRequest(200, userLocation.LocationId)
        ];

        Mock<IBedRequestDataService> bedRequestDataService = new();
        bedRequestDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        bedRequestDataService.Setup(x => x.LoadBedRequests(userLocation, null))
            .ReturnsAsync(new ServiceResponse<List<BedRequest>>("Found bed requests", true, bedRequests));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<BedRequest>> result = await controller.GetBedRequests(1, 1001);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<BedRequest> payload = okResult.Value as PageResponse<BedRequest>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.That(payload.ItemsPerPage, Is.EqualTo(1000));
    }

    [Test]
    public async Task GetBedRequests_ShouldReturnInternalServerError_WhenUserLocationCannotBeLoaded()
    {
        Mock<IBedRequestDataService> bedRequestDataService = new();
        bedRequestDataService.Setup(x => x.GetUserLocationId()).Returns(10);

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(new ServiceResponse<Location>("Unable to load user location"));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<BedRequest>> result = await controller.GetBedRequests(1, 1000);

        ApiError error = AssertApiErrorResponse(result.Result);
        Assert.That(error.Message, Is.EqualTo("Unable to load user location"));
        bedRequestDataService.Verify(
            x => x.LoadBedRequests(It.IsAny<Location>(), It.IsAny<List<Location>?>()), Times.Never);
    }

    [Test]
    public async Task GetBedRequests_ShouldReturnInternalServerError_WhenBedRequestServiceFails()
    {
        Location userLocation = CreateLocation(10);

        Mock<IBedRequestDataService> bedRequestDataService = new();
        bedRequestDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        bedRequestDataService.Setup(x => x.LoadBedRequests(userLocation, null))
            .ReturnsAsync(new ServiceResponse<List<BedRequest>>("Unable to load bed requests"));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<BedRequest>> result = await controller.GetBedRequests(1, 1000);

        ApiError error = AssertApiErrorResponse(result.Result);
        Assert.That(error.Message, Is.EqualTo("Unable to load bed requests"));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnBedRequest_WhenRequestIsInUserLocation()
    {
        Location userLocation = CreateLocation(10);
        BedRequest bedRequest = CreateBedRequest(100, userLocation.LocationId);
        Mock<IBedRequestDataService> bedRequestDataService = new();
        Mock<ILocationDataService> locationDataService = new();
        ConfigureUserLocation(bedRequestDataService, locationDataService, userLocation);
        bedRequestDataService.Setup(x => x.GetByIdAsync(bedRequest.BedRequestId))
            .ReturnsAsync(new ServiceResponse<BedRequest>("Found bed request", true, bedRequest));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<BedRequest> result = await controller.GetByIdAsync(bedRequest.BedRequestId);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        Assert.That(okResult.Value, Is.SameAs(bedRequest));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnForbidden_WhenRequestIsOutsideUserLocation()
    {
        Location userLocation = CreateLocation(10);
        BedRequest bedRequest = CreateBedRequest(100, 20);
        Mock<IBedRequestDataService> bedRequestDataService = new();
        Mock<ILocationDataService> locationDataService = new();
        ConfigureUserLocation(bedRequestDataService, locationDataService, userLocation);
        bedRequestDataService.Setup(x => x.GetByIdAsync(bedRequest.BedRequestId))
            .ReturnsAsync(new ServiceResponse<BedRequest>("Found bed request", true, bedRequest));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<BedRequest> result = await controller.GetByIdAsync(bedRequest.BedRequestId);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task CreateAsync_ShouldReturnCreated_WhenRequestIsInUserLocation()
    {
        Location userLocation = CreateLocation(10);
        BedRequest bedRequest = CreateBedRequest(100, userLocation.LocationId);
        Mock<IBedRequestDataService> bedRequestDataService = new();
        Mock<ILocationDataService> locationDataService = new();
        ConfigureUserLocation(bedRequestDataService, locationDataService, userLocation);
        bedRequestDataService.Setup(x => x.CreateAsync(bedRequest))
            .ReturnsAsync(new ServiceResponse<BedRequest>("Created bed request", true, bedRequest));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<BedRequest> result = await controller.CreateAsync(bedRequest);

        CreatedAtActionResult createdResult = result.Result as CreatedAtActionResult
            ?? throw new AssertionException("Expected a created response.");
        Assert.Multiple(() =>
        {
            Assert.That(createdResult.ActionName, Is.EqualTo(nameof(BedRequestsController.GetByIdAsync)));
            Assert.That(createdResult.RouteValues?["id"], Is.EqualTo(bedRequest.BedRequestId));
            Assert.That(createdResult.Value, Is.SameAs(bedRequest));
        });
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnOk_WhenRequestIsInUserLocation()
    {
        Location userLocation = CreateLocation(10);
        BedRequest bedRequest = CreateBedRequest(100, userLocation.LocationId);
        Mock<IBedRequestDataService> bedRequestDataService = new();
        Mock<ILocationDataService> locationDataService = new();
        ConfigureUserLocation(bedRequestDataService, locationDataService, userLocation);
        bedRequestDataService.Setup(x => x.GetByIdAsync(bedRequest.BedRequestId))
            .ReturnsAsync(new ServiceResponse<BedRequest>("Found bed request", true, bedRequest));
        bedRequestDataService.Setup(x => x.UpdateAsync(bedRequest))
            .ReturnsAsync(new ServiceResponse<BedRequest>("Updated bed request", true, bedRequest));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<BedRequest> result =
            await controller.UpdateAsync(bedRequest.BedRequestId, bedRequest);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        Assert.That(okResult.Value, Is.SameAs(bedRequest));
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnBadRequest_WhenRouteIdDoesNotMatch()
    {
        BedRequest bedRequest = CreateBedRequest(100, 10);
        Mock<IBedRequestDataService> bedRequestDataService = new();
        Mock<ILocationDataService> locationDataService = new();
        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<BedRequest> result = await controller.UpdateAsync(200, bedRequest);

        ObjectResult objectResult = result.Result as ObjectResult
            ?? throw new AssertionException("Expected a bad request response.");
        ApiError error = objectResult.Value as ApiError
            ?? throw new AssertionException("Expected an ApiError payload.");
        Assert.Multiple(() =>
        {
            Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(error.Message, Is.EqualTo("The route id must match the bed request id."));
        });
        bedRequestDataService.Verify(x => x.UpdateAsync(It.IsAny<BedRequest>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenRequestIsInUserLocation()
    {
        Location userLocation = CreateLocation(10);
        BedRequest bedRequest = CreateBedRequest(100, userLocation.LocationId);
        Mock<IBedRequestDataService> bedRequestDataService = new();
        Mock<ILocationDataService> locationDataService = new();
        ConfigureUserLocation(bedRequestDataService, locationDataService, userLocation);
        bedRequestDataService.Setup(x => x.GetByIdAsync(bedRequest.BedRequestId))
            .ReturnsAsync(new ServiceResponse<BedRequest>("Found bed request", true, bedRequest));
        bedRequestDataService.Setup(x => x.DeleteAsync(bedRequest.BedRequestId))
            .ReturnsAsync(new ServiceResponse<bool>("Deleted bed request", true, true));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        BedRequestsController controller =
            new(bedRequestDataService.Object, locationDataService.Object, configurationDataService.Object);

        IActionResult result = await controller.DeleteAsync(bedRequest.BedRequestId);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    private static void ConfigureUserLocation(Mock<IBedRequestDataService> bedRequestDataService,
        Mock<ILocationDataService> locationDataService, Location userLocation)
    {
        bedRequestDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));
    }

    private static BedRequest CreateBedRequest(int bedRequestId, int locationId)
    {
        return new BedRequest
        {
            BedRequestId = bedRequestId,
            LocationId = locationId,
            FirstName = "Test",
            NumberOfBeds = 1,
            PrimaryLanguage = "English"
        };
    }

    private static Location CreateLocation(int locationId, int? metroAreaId = null)
    {
        return new Location
        {
            LocationId = locationId,
            Name = $"Location {locationId}",
            Route = $"/location-{locationId}",
            BuildPostalCode = "43085",
            IsActive = true,
            MetroAreaId = metroAreaId
        };
    }

    private static ApiError AssertApiErrorResponse(IActionResult? result)
    {
        ObjectResult objectResult = result as ObjectResult
            ?? throw new AssertionException("Expected an error response.");

        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));

        return objectResult.Value as ApiError
            ?? throw new AssertionException("Expected an ApiError payload.");
    }
}
