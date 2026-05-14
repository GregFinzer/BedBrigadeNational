using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BedBrigade.Tests.Integration;

[TestFixture]
public class AuthDataServiceLockoutTests
{
    private const string Email = "lockout@example.com";
    private const string UserName = "lockout-user";
    private const string OriginalPassword = "CorrectPassword123!";
    private const string UpdatedPassword = "UpdatedPassword123!";

    [Test]
    public void AllConfigurationForSeeding_ShouldIncludeNationalLoginLockoutSettings()
    {
        List<Configuration> seededConfigurations = SeedConfigLogic.AllConfigurationForSeeding();

        Configuration? maxLoginAttempts = seededConfigurations.FirstOrDefault(c =>
            c.LocationId == Defaults.NationalLocationId
            && c.Section == ConfigSection.System
            && c.ConfigurationKey == ConfigNames.MaxLoginAttempts);
        Configuration? lockoutMinutes = seededConfigurations.FirstOrDefault(c =>
            c.LocationId == Defaults.NationalLocationId
            && c.Section == ConfigSection.System
            && c.ConfigurationKey == ConfigNames.LockoutMinutes);

        Assert.Multiple(() =>
        {
            Assert.That(maxLoginAttempts?.ConfigurationValue, Is.EqualTo("3"));
            Assert.That(lockoutMinutes?.ConfigurationValue, Is.EqualTo("30"));
        });
    }

    [Test]
    public async Task Login_ShouldLockUser_WhenMaxAttemptsAreReached()
    {
        var (authDataService, contextFactory) = CreateAuthDataService();
        await RegisterUserAsync(authDataService);

        await authDataService.Login(Email, "wrong-password-1");
        await authDataService.Login(Email, "wrong-password-2");
        ServiceResponse<System.Security.Claims.ClaimsPrincipal> result =
            await authDataService.Login(Email, "wrong-password-3");

        User savedUser = await GetUserAsync(contextFactory);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Your account is locked for 30 minutes."));
            Assert.That(savedUser.FailedLoginAttempts, Is.EqualTo(3));
            Assert.That(savedUser.LockoutEndUtc, Is.Not.Null);
            Assert.That(savedUser.LockoutEndUtc, Is.GreaterThan(DateTime.UtcNow.AddMinutes(29)));
        });
    }

    [Test]
    public async Task Login_ShouldShowRemainingLockoutTime_WhenUserAttemptsDuringLockout()
    {
        var (authDataService, _) = CreateAuthDataService();
        await RegisterUserAsync(authDataService);

        await authDataService.Login(Email, "wrong-password-1");
        await authDataService.Login(Email, "wrong-password-2");
        await authDataService.Login(Email, "wrong-password-3");

        ServiceResponse<System.Security.Claims.ClaimsPrincipal> lockedResult =
            await authDataService.Login(Email, OriginalPassword);

        Assert.Multiple(() =>
        {
            Assert.That(lockedResult.Success, Is.False);
            Assert.That(lockedResult.Message, Does.StartWith("Your account is locked for "));
            Assert.That(lockedResult.Message, Does.EndWith(" minutes."));
        });
    }

    [Test]
    public async Task ChangePassword_ShouldPreserveFailedAttemptsAndLockout_WhenUserIsStillLocked()
    {
        var (authDataService, contextFactory) = CreateAuthDataService();
        await RegisterUserAsync(authDataService);

        await authDataService.Login(Email, "wrong-password-1");
        await authDataService.Login(Email, "wrong-password-2");
        await authDataService.Login(Email, "wrong-password-3");

        ServiceResponse<User> passwordChangeResult =
            await authDataService.ChangePassword(UserName, UpdatedPassword, mustChangePassword: false);
        ServiceResponse<System.Security.Claims.ClaimsPrincipal> loginResult =
            await authDataService.Login(Email, UpdatedPassword);
        User savedUser = await GetUserAsync(contextFactory);

        Assert.Multiple(() =>
        {
            Assert.That(passwordChangeResult.Success, Is.True);
            Assert.That(loginResult.Success, Is.False);
            Assert.That(loginResult.Message, Does.StartWith("Your account is locked for "));
            Assert.That(savedUser.FailedLoginAttempts, Is.EqualTo(3));
            Assert.That(savedUser.LockoutEndUtc, Is.Not.Null);
        });
    }

    [Test]
    public async Task Login_ShouldSucceedWithUpdatedPassword_AfterLockoutExpires()
    {
        var (authDataService, contextFactory) = CreateAuthDataService();
        await RegisterUserAsync(authDataService);

        await authDataService.Login(Email, "wrong-password-1");
        await authDataService.Login(Email, "wrong-password-2");
        await authDataService.Login(Email, "wrong-password-3");
        await authDataService.ChangePassword(UserName, UpdatedPassword, mustChangePassword: false);
        await SetLockoutEndUtcAsync(contextFactory, DateTime.UtcNow.AddMinutes(-1));

        ServiceResponse<System.Security.Claims.ClaimsPrincipal> loginResult =
            await authDataService.Login(Email, UpdatedPassword);
        User savedUser = await GetUserAsync(contextFactory);

        Assert.Multiple(() =>
        {
            Assert.That(loginResult.Success, Is.True, loginResult.Message);
            Assert.That(savedUser.FailedLoginAttempts, Is.EqualTo(0));
            Assert.That(savedUser.LockoutEndUtc, Is.Null);
        });
    }

    private static (AuthDataService AuthDataService, IDbContextFactory<DataContext> ContextFactory) CreateAuthDataService()
    {
        DbContextOptions<DataContext> options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        IDbContextFactory<DataContext> contextFactory = new TestDataContextFactory(options);
        ICachingService cachingService = new CachingService { IsCachingEnabled = false };
        Mock<ILocationDataService> locationDataService = new();
        Mock<IConfigurationDataService> configurationDataService = new();

        locationDataService.Setup(x => x.GetByIdAsync(It.Is<object>(value => value is int)))
            .ReturnsAsync(new ServiceResponse<Location>("Location found", true, new Location
            {
                LocationId = Defaults.NationalLocationId,
                Name = "National",
                Route = "national",
                TimeZoneId = Defaults.DefaultTimeZoneId
            }));

        configurationDataService
            .Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.MaxLoginAttempts))
            .ReturnsAsync(3);
        configurationDataService
            .Setup(x => x.GetConfigValueAsIntAsync(ConfigSection.System, ConfigNames.LockoutMinutes))
            .ReturnsAsync(30);

        AuthDataService authDataService = new(contextFactory,
            locationDataService.Object,
            cachingService,
            configurationDataService.Object);

        return (authDataService, contextFactory);
    }

    private static async Task RegisterUserAsync(AuthDataService authDataService)
    {
        User user = new()
        {
            UserName = UserName,
            Email = Email,
            FirstName = "Lockout",
            LastName = "Tester",
            Phone = "6145551212",
            Role = RoleNames.NationalAdmin,
            FkRole = 1,
            LocationId = Defaults.NationalLocationId
        };

        ServiceResponse<bool> registerResult = await authDataService.Register(user, OriginalPassword);
        Assert.That(registerResult.Success, Is.True, registerResult.Message);
    }

    private static async Task<User> GetUserAsync(IDbContextFactory<DataContext> contextFactory)
    {
        using DataContext context = contextFactory.CreateDbContext();
        User? user = await context.Users.SingleOrDefaultAsync(x => x.UserName == UserName);

        return user ?? throw new InvalidOperationException($"Could not find user {UserName}.");
    }

    private static async Task SetLockoutEndUtcAsync(IDbContextFactory<DataContext> contextFactory, DateTime lockoutEndUtc)
    {
        using DataContext context = contextFactory.CreateDbContext();
        User user = await GetUserAsync(contextFactory);

        user.LockoutEndUtc = lockoutEndUtc;
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }
}



