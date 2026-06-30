using System.Reflection;
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
public class RepositoryControllersTests
{
    [TestCase(typeof(MetroAreasController), nameof(MetroAreasController.GetAllAsync), RoleNames.CanViewMetroAreas)]
    [TestCase(typeof(MetroAreasController), nameof(MetroAreasController.GetByIdAsync), RoleNames.CanViewMetroAreas)]
    [TestCase(typeof(MetroAreasController), nameof(MetroAreasController.CreateAsync), RoleNames.NationalAdmin)]
    [TestCase(typeof(MetroAreasController), nameof(MetroAreasController.UpdateAsync), RoleNames.NationalAdmin)]
    [TestCase(typeof(MetroAreasController), nameof(MetroAreasController.DeleteAsync), RoleNames.NationalAdmin)]
    [TestCase(typeof(ContactUsController), nameof(ContactUsController.GetAllAsync), RoleNames.CanViewContacts)]
    [TestCase(typeof(ContactUsController), nameof(ContactUsController.CreateAsync), RoleNames.CanManageContacts)]
    [TestCase(typeof(VolunteersController), nameof(VolunteersController.GetAllAsync), RoleNames.CanViewVolunteers)]
    [TestCase(typeof(VolunteersController), nameof(VolunteersController.CreateAsync), RoleNames.CanManageVolunteers)]
    [TestCase(typeof(SchedulesController), nameof(SchedulesController.GetAllAsync), RoleNames.CanViewSchedule)]
    [TestCase(typeof(SchedulesController), nameof(SchedulesController.CreateAsync), RoleNames.CanManageSchedule)]
    [TestCase(typeof(SignUpsController), nameof(SignUpsController.GetAllAsync), RoleNames.CanViewSignUps)]
    [TestCase(typeof(SignUpsController), nameof(SignUpsController.CreateAsync), RoleNames.CanManageSchedule)]
    [TestCase(typeof(UsersController), nameof(UsersController.GetAllAsync), RoleNames.CanViewUsers)]
    [TestCase(typeof(UsersController), nameof(UsersController.CreateAsync), RoleNames.CanManageUsers)]
    [TestCase(typeof(DonationsController), nameof(DonationsController.GetAllAsync), RoleNames.CanManageDonations)]
    [TestCase(typeof(DonationsController), nameof(DonationsController.CreateAsync), RoleNames.CanManageDonations)]
    [TestCase(typeof(DonationCampaignsController), nameof(DonationCampaignsController.GetAllAsync), RoleNames.CanManageDonationCampaigns)]
    [TestCase(typeof(DonationCampaignsController), nameof(DonationCampaignsController.CreateAsync), RoleNames.CanManageDonationCampaigns)]
    [TestCase(typeof(ContentsController), nameof(ContentsController.GetAllAsync), RoleNames.CanViewPages)]
    [TestCase(typeof(ContentsController), nameof(ContentsController.CreateAsync), RoleNames.CanManagePages)]
    [TestCase(typeof(NewslettersController), nameof(NewslettersController.GetAllAsync), RoleNames.CanManageNewsletters)]
    [TestCase(typeof(NewslettersController), nameof(NewslettersController.CreateAsync), RoleNames.CanManageNewsletters)]
    public void ControllerAction_ShouldUseExpectedRole(Type controllerType, string actionName, string expectedRoles)
    {
        MethodInfo method = controllerType.GetMethods()
            .Single(method => method.Name == actionName);

        AuthorizeAttribute authorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>()
            ?? throw new AssertionException($"Expected {controllerType.Name}.{actionName} to have Authorize.");

        Assert.That(authorizeAttribute.Roles, Is.EqualTo(expectedRoles));
    }

    [Test]
    public async Task MetroAreasController_ShouldSupportCrud()
    {
        MetroArea metroArea = new() { MetroAreaId = 7, Name = "Central Ohio" };
        List<MetroArea> metroAreas = [metroArea];
        Mock<IMetroAreaDataService> dataService = new();
        dataService.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new ServiceResponse<List<MetroArea>>("Found metro areas", true, metroAreas));
        dataService.Setup(x => x.GetByIdAsync(7))
            .ReturnsAsync(new ServiceResponse<MetroArea>("Found metro area", true, metroArea));
        dataService.Setup(x => x.CreateAsync(metroArea))
            .ReturnsAsync(new ServiceResponse<MetroArea>("Created metro area", true, metroArea));
        dataService.Setup(x => x.UpdateAsync(metroArea))
            .ReturnsAsync(new ServiceResponse<MetroArea>("Updated metro area", true, metroArea));
        dataService.Setup(x => x.DeleteAsync(7))
            .ReturnsAsync(new ServiceResponse<bool>("Deleted metro area", true, true));
        MetroAreasController controller = new(dataService.Object);

        ActionResult<List<MetroArea>> getAllResult = await controller.GetAllAsync();
        ActionResult<MetroArea> getByIdResult = await controller.GetByIdAsync(7);
        ActionResult<MetroArea> createResult = await controller.CreateAsync(metroArea);
        ActionResult<MetroArea> updateResult = await controller.UpdateAsync(7, metroArea);
        IActionResult deleteResult = await controller.DeleteAsync(7);

        Assert.Multiple(() =>
        {
            Assert.That((getAllResult.Result as OkObjectResult)?.Value, Is.SameAs(metroAreas));
            Assert.That((getByIdResult.Result as OkObjectResult)?.Value, Is.SameAs(metroArea));
            Assert.That((createResult.Result as CreatedAtActionResult)?.RouteValues?["id"], Is.EqualTo(7));
            Assert.That((updateResult.Result as OkObjectResult)?.Value, Is.SameAs(metroArea));
            Assert.That(deleteResult, Is.TypeOf<NoContentResult>());
        });
    }

    [Test]
    public async Task VolunteersController_ShouldFilterListToUserMetroArea()
    {
        Location userLocation = CreateLocation(10, 2);
        Location metroLocation = CreateLocation(20, 2);
        List<Volunteer> volunteers =
        [
            new Volunteer { VolunteerId = 1, LocationId = 10, FirstName = "One" },
            new Volunteer { VolunteerId = 2, LocationId = 20, FirstName = "Two" },
            new Volunteer { VolunteerId = 3, LocationId = 30, FirstName = "Three" }
        ];
        Mock<IVolunteerDataService> dataService = new();
        dataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        dataService.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new ServiceResponse<List<Volunteer>>("Found volunteers", true, volunteers));
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));
        locationDataService.Setup(x => x.GetLocationsByMetroAreaId(2))
            .ReturnsAsync(new ServiceResponse<List<Location>>("Found metro locations", true,
                [userLocation, metroLocation]));
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();
        VolunteersController controller = new(dataService.Object, locationDataService.Object,
            configurationDataService.Object);

        ActionResult<PageResponse<Volunteer>> result = await controller.GetAllAsync(1, 10);

        OkObjectResult okResult = result.Result as OkObjectResult
            ?? throw new AssertionException("Expected an OK response.");
        PageResponse<Volunteer> payload = okResult.Value as PageResponse<Volunteer>
            ?? throw new AssertionException("Expected volunteer page payload.");
        Assert.That(payload.Items.Select(x => x.VolunteerId), Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task VolunteersController_ShouldReturnForbidden_WhenCreatingOutsideLocationScope()
    {
        Location userLocation = CreateLocation(10);
        Volunteer volunteer = new() { VolunteerId = 1, LocationId = 20, FirstName = "One" };
        Mock<IVolunteerDataService> dataService = new();
        dataService.Setup(x => x.GetUserLocationId()).Returns(userLocation.LocationId);
        Mock<ILocationDataService> locationDataService = new();
        locationDataService.Setup(x => x.GetByIdAsync(userLocation.LocationId))
            .ReturnsAsync(new ServiceResponse<Location>("Found location", true, userLocation));
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();
        VolunteersController controller = new(dataService.Object, locationDataService.Object,
            configurationDataService.Object);

        ActionResult<Volunteer> result = await controller.CreateAsync(volunteer);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
        dataService.Verify(x => x.CreateAsync(It.IsAny<Volunteer>()), Times.Never);
    }

    [Test]
    public async Task ContentsController_ShouldUseNonBlogListForNationalAdmin()
    {
        List<Content> contents = [new Content { ContentId = 1, LocationId = 10, Name = "Home", Title = "Home" }];
        Mock<IContentDataService> dataService = new();
        dataService.Setup(x => x.IsUserNationalAdmin()).Returns(true);
        dataService.Setup(x => x.GetAllExceptBlogTypes())
            .ReturnsAsync(new ServiceResponse<List<Content>>("Found content", true, contents));
        Mock<ILocationDataService> locationDataService = new();
        Mock<IConfigurationDataService> configurationDataService = CreatePagingConfigurationDataService();
        ContentsController controller = new(dataService.Object, locationDataService.Object,
            configurationDataService.Object);

        ActionResult<PageResponse<Content>> result = await controller.GetAllAsync(1, 10);

        PageResponse<Content> payload = (result.Result as OkObjectResult)?.Value as PageResponse<Content>
            ?? throw new AssertionException("Expected content page payload.");
        Assert.That(payload.Items, Is.EqualTo(contents));
        dataService.Verify(x => x.GetAllExceptBlogTypes(), Times.Once);
    }
    
    private static Mock<IConfigurationDataService> CreatePagingConfigurationDataService()
    {
        Mock<IConfigurationDataService> configurationDataService = new();
        configurationDataService.Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxItemsPerPage))
            .ReturnsAsync(1000);
        return configurationDataService;
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
}
