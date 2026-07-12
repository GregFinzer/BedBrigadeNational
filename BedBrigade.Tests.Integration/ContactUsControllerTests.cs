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
public class ContactUsControllerTests
{
    [Test]
    public void ContactUsController_ShouldUseLocationScopedRepositoryControllerPattern()
    {
        Assert.That(typeof(ContactUsController).BaseType,
            Is.EqualTo(typeof(LocationScopedRepositoryControllerBase<ContactUs, int, IContactUsDataService>)));
    }

    [TestCase(nameof(ContactUsController.GetAllAsync), RoleNames.CanViewContacts)]
    [TestCase(nameof(ContactUsController.GetContactUsByStatus), RoleNames.CanViewContacts)]
    [TestCase(nameof(ContactUsController.GetByIdAsync), RoleNames.CanViewContacts)]
    [TestCase(nameof(ContactUsController.CreateAsync), RoleNames.CanManageContacts)]
    [TestCase(nameof(ContactUsController.UpdateAsync), RoleNames.CanManageContacts)]
    [TestCase(nameof(ContactUsController.DeleteAsync), RoleNames.CanManageContacts)]
    public void ControllerAction_ShouldUseExpectedRole(string actionName, string expectedRoles)
    {
        var method = typeof(ContactUsController).GetMethods()
            .Single(method => method.Name == actionName);

        AuthorizeAttribute authorizeAttribute = method.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.That(authorizeAttribute.Roles, Is.EqualTo(expectedRoles));
    }

    [Test]
    public async Task GetContactUsByStatus_ShouldReturnRequestsFilteredByStatusForUserLocation()
    {
        Location userLocation = CreateLocation(10);
        List<ContactUs> allContactUsRequests =
        [
            new ContactUs { ContactUsId = 100, LocationId = userLocation.LocationId, Status = ContactUsStatus.ContactRequested },
            new ContactUs { ContactUsId = 200, LocationId = userLocation.LocationId, Status = ContactUsStatus.Responded },
            new ContactUs { ContactUsId = 300, LocationId = userLocation.LocationId, Status = ContactUsStatus.Cancelled }
        ];

        Mock<IContactUsDataService> contactUsDataService = new();
        contactUsDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        contactUsDataService.Setup(x => x.GetContactUsByUserAndStatus(It.IsAny<List<ContactUsStatus>>()))
            .ReturnsAsync(new ServiceResponse<List<ContactUs>>("Found contact-us requests", true, 
                allContactUsRequests.Where(cu => cu.Status == ContactUsStatus.ContactRequested).ToList()));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        ContactUsController controller =
            new(contactUsDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<ContactUs>> result = await controller.GetContactUsByStatus(1, 1000, 
            new List<ContactUsStatus> { ContactUsStatus.ContactRequested });

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<ContactUs> payload = okResult.Value as PageResponse<ContactUs>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.Multiple(() =>
        {
            Assert.That(payload.PageNumber, Is.EqualTo(1));
            Assert.That(payload.NumberOfItems, Is.EqualTo(1));
            Assert.That(payload.Items, Has.Count.EqualTo(1));
            Assert.That(payload.Items.First().Status, Is.EqualTo(ContactUsStatus.ContactRequested));
        });
        contactUsDataService.Verify(x => x.GetContactUsByUserAndStatus(
            It.Is<List<ContactUsStatus>>(s => s.Contains(ContactUsStatus.ContactRequested))), Times.Once);
    }

    [Test]
    public async Task GetContactUsByStatus_ShouldReturnRequestsFilteredByStatusForMetroArea()
    {
        Location userLocation = CreateLocation(10, 5);
        List<Location> metroLocations =
        [
            userLocation,
            CreateLocation(20, 5)
        ];
        List<ContactUs> allContactUsRequests =
        [
            new ContactUs { ContactUsId = 100, LocationId = 10, Status = ContactUsStatus.ContactRequested },
            new ContactUs { ContactUsId = 200, LocationId = 10, Status = ContactUsStatus.Responded },
            new ContactUs { ContactUsId = 300, LocationId = 20, Status = ContactUsStatus.ContactRequested },
            new ContactUs { ContactUsId = 400, LocationId = 20, Status = ContactUsStatus.Cancelled }
        ];

        Mock<IContactUsDataService> contactUsDataService = new();
        contactUsDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        contactUsDataService.Setup(x => x.GetContactUsByUserAndStatus(It.IsAny<List<ContactUsStatus>>()))
            .ReturnsAsync(new ServiceResponse<List<ContactUs>>("Found contact-us requests", true, 
                allContactUsRequests.Where(cu => cu.Status == ContactUsStatus.ContactRequested).ToList()));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));
        locationDataService.Setup(x => x.GetLocationsByMetroAreaId(5))
            .ReturnsAsync(new ServiceResponse<List<Location>>("Found metro locations", true, metroLocations));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        ContactUsController controller =
            new(contactUsDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<ContactUs>> result = await controller.GetContactUsByStatus(1, 1000, 
            new List<ContactUsStatus> { ContactUsStatus.ContactRequested });

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<ContactUs> payload = okResult.Value as PageResponse<ContactUs>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.That(payload.Items, Has.Count.EqualTo(2));
        Assert.That(payload.Items.All(cu => cu.Status == ContactUsStatus.ContactRequested), Is.True);
        contactUsDataService.Verify(x => x.GetContactUsByUserAndStatus(
            It.Is<List<ContactUsStatus>>(s => s.Contains(ContactUsStatus.ContactRequested))), Times.Once);
    }

    [Test]
    public async Task GetContactUsByStatus_ShouldReturnRequestsWithMultipleStatuses()
    {
        Location userLocation = CreateLocation(10);
        List<ContactUs> allContactUsRequests =
        [
            new ContactUs { ContactUsId = 100, LocationId = userLocation.LocationId, Status = ContactUsStatus.ContactRequested },
            new ContactUs { ContactUsId = 200, LocationId = userLocation.LocationId, Status = ContactUsStatus.Responded },
            new ContactUs { ContactUsId = 300, LocationId = userLocation.LocationId, Status = ContactUsStatus.Cancelled }
        ];
        
        List<ContactUsStatus> requestedStatuses = new() { ContactUsStatus.ContactRequested, ContactUsStatus.Responded };

        Mock<IContactUsDataService> contactUsDataService = new();
        contactUsDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        contactUsDataService.Setup(x => x.GetContactUsByUserAndStatus(It.IsAny<List<ContactUsStatus>>()))
            .ReturnsAsync(new ServiceResponse<List<ContactUs>>("Found contact-us requests", true, 
                allContactUsRequests.Where(cu => requestedStatuses.Contains(cu.Status)).ToList()));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        ContactUsController controller =
            new(contactUsDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<ContactUs>> result = await controller.GetContactUsByStatus(1, 1000, requestedStatuses);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<ContactUs> payload = okResult.Value as PageResponse<ContactUs>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.Multiple(() =>
        {
            Assert.That(payload.NumberOfItems, Is.EqualTo(2));
            Assert.That(payload.Items, Has.Count.EqualTo(2));
            Assert.That(payload.Items.All(cu => requestedStatuses.Contains(cu.Status)), Is.True);
        });
    }

    [Test]
    public async Task GetContactUsByStatus_ShouldReturnEmptyListWhenNoMatchingStatuses()
    {
        Location userLocation = CreateLocation(10);
        List<ContactUs> allContactUsRequests =
        [
            new ContactUs { ContactUsId = 100, LocationId = userLocation.LocationId, Status = ContactUsStatus.ContactRequested },
            new ContactUs { ContactUsId = 200, LocationId = userLocation.LocationId, Status = ContactUsStatus.Responded }
        ];

        Mock<IContactUsDataService> contactUsDataService = new();
        contactUsDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        contactUsDataService.Setup(x => x.GetContactUsByUserAndStatus(It.IsAny<List<ContactUsStatus>>()))
            .ReturnsAsync(new ServiceResponse<List<ContactUs>>("Found contact-us requests", true, 
                allContactUsRequests.Where(cu => cu.Status == ContactUsStatus.Cancelled).ToList()));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        ContactUsController controller =
            new(contactUsDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<ContactUs>> result = await controller.GetContactUsByStatus(1, 1000, 
            new List<ContactUsStatus> { ContactUsStatus.Cancelled });

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<ContactUs> payload = okResult.Value as PageResponse<ContactUs>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.That(payload.Items, Is.Empty);
        Assert.That(payload.NumberOfItems, Is.EqualTo(0));
    }

    [Test]
    public async Task GetContactUsByStatus_ShouldReturnInternalServerError_WhenContactUsServiceFails()
    {
        Location userLocation = CreateLocation(10);

        Mock<IContactUsDataService> contactUsDataService = new();
        contactUsDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        contactUsDataService.Setup(x => x.GetContactUsByUserAndStatus(It.IsAny<List<ContactUsStatus>>()))
            .ReturnsAsync(new ServiceResponse<List<ContactUs>>("Unable to load contact-us requests"));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        ContactUsController controller =
            new(contactUsDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<ContactUs>> result = await controller.GetContactUsByStatus(1, 1000, 
            new List<ContactUsStatus> { ContactUsStatus.ContactRequested });

        ApiError error = AssertApiErrorResponse(result.Result);
        Assert.That(error.Message, Is.EqualTo("Unable to load contact-us requests"));
    }

    [Test]
    public async Task GetContactUsByStatus_ShouldReturnRequestedPage()
    {
        Location userLocation = CreateLocation(10);
        List<ContactUs> contactUsRequests =
        [
            CreateContactUs(100, userLocation.LocationId, ContactUsStatus.ContactRequested),
            CreateContactUs(200, userLocation.LocationId, ContactUsStatus.ContactRequested),
            CreateContactUs(300, userLocation.LocationId, ContactUsStatus.ContactRequested)
        ];

        Mock<IContactUsDataService> contactUsDataService = new();
        contactUsDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        contactUsDataService.Setup(x => x.GetContactUsByUserAndStatus(It.IsAny<List<ContactUsStatus>>()))
            .ReturnsAsync(new ServiceResponse<List<ContactUs>>("Found contact-us requests", true, contactUsRequests));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        ContactUsController controller =
            new(contactUsDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<ContactUs>> result = await controller.GetContactUsByStatus(2, 2, 
            new List<ContactUsStatus> { ContactUsStatus.ContactRequested });

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<ContactUs> payload = okResult.Value as PageResponse<ContactUs>
            ?? throw new AssertionException("Expected a page response payload.");

        Assert.Multiple(() =>
        {
            Assert.That(payload.PageNumber, Is.EqualTo(2));
            Assert.That(payload.MaxPage, Is.EqualTo(2));
            Assert.That(payload.NumberOfItems, Is.EqualTo(3));
            Assert.That(payload.ItemsPerPage, Is.EqualTo(2));
            Assert.That(payload.Items.Select(x => x.ContactUsId), Is.EqualTo(new[] { 300 }));
        });
    }

    [Test]
    public async Task GetContactUsByStatus_ShouldCapItemsPerPageAtOneThousand()
    {
        Location userLocation = CreateLocation(10);
        List<ContactUs> contactUsRequests =
        [
            CreateContactUs(100, userLocation.LocationId, ContactUsStatus.ContactRequested),
            CreateContactUs(200, userLocation.LocationId, ContactUsStatus.ContactRequested)
        ];

        Mock<IContactUsDataService> contactUsDataService = new();
        contactUsDataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        contactUsDataService.Setup(x => x.GetContactUsByUserAndStatus(It.IsAny<List<ContactUsStatus>>()))
            .ReturnsAsync(new ServiceResponse<List<ContactUs>>("Found contact-us requests", true, contactUsRequests));

        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));

        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        
        ContactUsController controller =
            new(contactUsDataService.Object, locationDataService.Object, configurationDataService.Object);

        ActionResult<PageResponse<ContactUs>> result = await controller.GetContactUsByStatus(1, 1001, 
            new List<ContactUsStatus> { ContactUsStatus.ContactRequested });

        ObjectResult objectResult = result.Result as ObjectResult
                                    ?? throw new AssertionException("Expected an error response.");
        
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        
        ApiError apiError = objectResult.Value as ApiError
               ?? throw new AssertionException("Expected an ApiError payload.");
        
        Assert.That(apiError.Message, Is.EqualTo("itemsPerPage must be between 1 and 1000."));
    }

    private static ContactUs CreateContactUs(int contactUsId, int locationId, ContactUsStatus status = ContactUsStatus.ContactRequested)
    {
        return new ContactUs
        {
            ContactUsId = contactUsId,
            LocationId = locationId,
            FirstName = "John",
            LastName = "Doe",
            Email = $"test{contactUsId}@example.com",
            Phone = "(555) 555-5555",
            Message = "Test message",
            Status = status
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
